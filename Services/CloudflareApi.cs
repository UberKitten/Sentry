using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Sentry.Services
{
    class CloudflareApi : BaseService
    {
        public class ServiceOptions
        {
            public string Email { get; set; }
            public string ApiKey { get; set; }
            public List<string> ZoneIds { get; set; }
            public List<string> RecordRegexes { get; set; }
            public Dictionary<string, string> UpdateValues { get; set; }
        };

        protected ServiceOptions Options { get; set; }

        public class ServiceTriggerCriteria
        {
            // Currently no triggers, only actions
        };

        protected RestClient client = new RestClient("http://api.cloudflare.com/client/");
        
        public CloudflareApi(SecretsStoreManager secretsStoreManager, string id, object ServiceOptions) : base(secretsStoreManager, id)
        {
            Options = (ServiceOptions)ServiceOptions;

            client.AddDefaultHeader("X-Auth-Email", secretsStoreManager.TryGetSecret(Options.Email));
            client.AddDefaultHeader("X-Auth-Key", secretsStoreManager.TryGetSecret(Options.ApiKey));
            client.AddDefaultHeader("Content-Type", "application/json; charset=utf-8");
            client.AddHandler("application/json", new DynamicJsonDeserializer());
        }

        private IRestResponse<dynamic> GetRecords(string zoneId)
        {
            var request = new RestRequest("/v4/zones/{zoneId}/dns_records", Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddUrlSegment("zoneId", zoneId);
            request.AddParameter("per_page", "100", ParameterType.RequestBody);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("GetRecords response: {0}", response.Content);
            return response;
        }
        
        private IRestResponse<dynamic> UpdateRecord(string zoneId, string recordId, string type, string name, string content, bool proxied)
        {
            var request = new RestRequest("/v4/zones/{zoneId}/dns_records/{recordId}", Method.PUT);
            request.RequestFormat = DataFormat.Json;
            request.AddUrlSegment("zoneId", zoneId);
            request.AddUrlSegment("recordId", recordId);
            request.AddBody(new { type, name, content, proxied });

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("UpdateRecord response: {0}", response.Content);
            return response;
        }


        private IRestResponse<dynamic> DeleteRecord(string zoneId, string recordId)
        {
            var request = new RestRequest("/v4/zones/{zoneId}/dns_records/{recordId}", Method.DELETE);
            request.RequestFormat = DataFormat.Json;
            request.AddUrlSegment("zoneId", zoneId);
            request.AddUrlSegment("recordId", recordId);

            IRestResponse<dynamic> response = client.Execute<dynamic>(request);
            logger.Trace("DeleteRecord response: {0}", response.Content);
            return response;
        }

        public override void Verify()
        {
            foreach (var zoneId in Options.ZoneIds)
            {
                var response = GetRecords(zoneId);
                if (response.Data.success != "True")
                {
                    logger.Error("Unable to retrieve records for Cloudflare zone ID {0}", zoneId);
                    throw new UnableToVerifyException();
                }
            }
        }

        public override void Action(List<string> actions)
        {
            // Not sure why you would put update and delete in actions, but just in case check update first
            if (actions.Contains("update", StringComparer.InvariantCultureIgnoreCase))
            {
                // The Dictionary created by Json.NET is case sensitive, but we want to allow "a" and "A"
                // We could use a CustomCreationConverter, but that may have unintended side effects on other services
                // So we just convert it here
                var caseInsensitiveUpdateValues = new Dictionary<string, string>(Options.UpdateValues, StringComparer.InvariantCultureIgnoreCase);

                foreach (var zoneId in Options.ZoneIds)
                {
                    var getRecordsResponse = GetRecords(zoneId);
                    if (getRecordsResponse.Data.success != "True")
                    {
                        logger.Error("Unable to retrieve records for Cloudflare zone ID {0}", zoneId);
                    }
                    else
                    {
                        logger.Trace("Processing zone ID {0}", zoneId);
                        foreach (var record in getRecordsResponse.Data.result)
                        {
                            // Create a single string representation of record to match against
                            var concatRecord = String.Format("{0} {1} {2} {3}", record.id, record.type, record.name, record.content);
                            logger.Trace("Record found: {0}", concatRecord);

                            foreach (var regex in Options.RecordRegexes)
                            {
                                var match = Regex.Match(concatRecord, regex);
                                if (match.Success)
                                {
                                    logger.Trace("Matched regex {0} with substring: {1}", regex, match.Value);
                                    logger.Info("Updating record {0}", concatRecord);

                                    var updateContent = caseInsensitiveUpdateValues[record.type.Value];

                                    bool proxied = record.proxied.Value;
                                    if (record.type.Value.Equals("a", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // If it's an RFC 1918 address for an A record, we have to set proxied to false or Cloudflare will reject
                                        var ip = IPAddress.Parse(updateContent);
                                        if (IPIsInternal(ip))
                                        {
                                            proxied = false;
                                        }
                                    }

                                    IRestResponse<dynamic> updateRecordResponse = UpdateRecord(zoneId, record.id.Value, record.type.Value, record.name.Value, updateContent, proxied);

                                    if (updateRecordResponse.Data.success != "True")
                                    {
                                        logger.Error("Error updating record {0}", concatRecord);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            if (actions.Contains("delete", StringComparer.InvariantCultureIgnoreCase))
            {
                foreach (var zoneId in Options.ZoneIds)
                {
                    var getRecordsResponse = GetRecords(zoneId);
                    if (getRecordsResponse.Data.success != "True")
                    {
                        logger.Error("Unable to retrieve records for Cloudflare zone ID {0}", zoneId);
                    }
                    else
                    {
                        logger.Trace("Processing zone ID {0}", zoneId);
                        foreach (var record in getRecordsResponse.Data.result)
                        {
                            // Create a single string representation of record to match against
                            var concatRecord = String.Format("{0} {1} {2} {3}", record.id, record.type, record.name, record.content);
                            logger.Trace("Record found: {0}", concatRecord);

                            foreach (var regex in Options.RecordRegexes)
                            {
                                var match = Regex.Match(concatRecord, regex);
                                if (match.Success)
                                {
                                    logger.Trace("Matched regex {0} with substring: {1}", regex, match.Value);

                                    logger.Info("Deleting record {0}", concatRecord);
                                    var deleteRecordResponse = DeleteRecord(zoneId, record.id.Value);

                                    if (deleteRecordResponse.Data.success != "True")
                                    {
                                        logger.Error("Error updating record {0}", concatRecord);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IPIsInternal(IPAddress toTest)
        {
            // Based off https://stackoverflow.com/a/39120248

            // Works for both families
            if (IPAddress.IsLoopback(toTest))
            {
                return true;
            }

            if (toTest.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                byte[] bytes = toTest.GetAddressBytes();
                switch (bytes[0])
                {
                    case 10:
                        return true;
                    case 172:
                        return bytes[1] < 32 && bytes[1] >= 16;
                    case 192:
                        return bytes[1] == 168;
                    default:
                        return false;
                }
            }
            else
            {
                // Don't think Cloudflare will allow proxying any of these
                return toTest.IsIPv6LinkLocal || toTest.IsIPv6Multicast || toTest.IsIPv6SiteLocal || toTest.IsIPv6Teredo;
            }
        }
    }
}
