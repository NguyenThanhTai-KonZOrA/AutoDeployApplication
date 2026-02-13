using ClientLauncher.Implement.Services.Interface;
using ClientLauncher.Implement.ViewModels.Request;
using ClientLauncher.Implement.ViewModels.Response;
using Microsoft.Extensions.Logging;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net.NetworkInformation;

namespace ClientLauncher.Implement.Services
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly string? _domainName;
        private readonly bool _usePrincipalContextFallback;

        public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger)
        {
            _logger = logger;
            _domainName = Environment.UserDomainName;
            _usePrincipalContextFallback = true;
        }

        public async Task<ADComputerListResponse> GetAllComputersAsync(ADComputerSearchRequest? request = null)
        {
            return await Task.Run(() =>
            {
                var response = new ADComputerListResponse();

                try
                {
                    string ldapPath = GetLdapPath(request?.OrganizationalUnit);

                    using var entry = new DirectoryEntry(ldapPath);
                    using var searcher = new DirectorySearcher(entry);

                    searcher.Filter = BuildComputerFilter(request);
                    ConfigureSearchProperties(searcher);
                    searcher.PageSize = 1000;
                    searcher.ReferralChasing = ReferralChasingOption.All;
                    searcher.SearchScope = SearchScope.Subtree;

                    var results = searcher.FindAll();

                    foreach (SearchResult result in results)
                    {
                        try
                        {
                            var computer = MapToADComputer(result);

                            if (request?.CheckOnlineStatus == true)
                            {
                                computer.IsOnline = IsComputerOnlineAsync(computer.DnsHostName ?? computer.Name).Result;
                            }

                            response.Computers.Add(computer);

                            if (computer.Enabled)
                                response.EnabledCount++;

                            if (computer.IsOnline)
                                response.OnlineCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing computer entry");
                        }
                    }

                    response.TotalCount = response.Computers.Count;

                    _logger.LogInformation("Retrieved {Count} computers from Active Directory", response.TotalCount);
                }
                catch (DirectoryServicesCOMException comEx) when (comEx.ErrorCode == -2147016357)
                {
                    _logger.LogWarning(comEx, "LDAP referral error, falling back to PrincipalContext method");

                    if (_usePrincipalContextFallback && string.IsNullOrWhiteSpace(request?.OrganizationalUnit))
                    {
                        return GetComputersUsingPrincipalContext(request);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error querying Active Directory for computers");
                    throw;
                }

                return response;
            });
        }

        public async Task<ADComputerResponse?> GetComputerByNameAsync(string computerName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using var context = new PrincipalContext(ContextType.Domain, _domainName);
                    using var computerPrincipal = ComputerPrincipal.FindByIdentity(context, computerName);

                    if (computerPrincipal == null)
                        return null;

                    var computer = new ADComputerResponse
                    {
                        Name = computerPrincipal.Name ?? string.Empty,
                        DistinguishedName = computerPrincipal.DistinguishedName,
                        Enabled = computerPrincipal.Enabled ?? false,
                        LastLogon = computerPrincipal.LastLogon,
                        Description = computerPrincipal.Description
                    };

                    var directoryEntry = computerPrincipal.GetUnderlyingObject() as DirectoryEntry;
                    if (directoryEntry != null)
                    {
                        computer.DnsHostName = GetPropertyValue(directoryEntry, "dNSHostName");
                        computer.OperatingSystem = GetPropertyValue(directoryEntry, "operatingSystem");
                        computer.OperatingSystemVersion = GetPropertyValue(directoryEntry, "operatingSystemVersion");
                        computer.Location = GetPropertyValue(directoryEntry, "location");
                    }

                    computer.IsOnline = await IsComputerOnlineAsync(computer.DnsHostName ?? computer.Name);

                    return computer;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting computer {ComputerName} from AD", computerName);
                    return null;
                }
            });
        }

        public async Task<List<ADOrganizationalUnitResponse>> GetOrganizationalUnitsAsync()
        {
            return await Task.Run(() =>
            {
                var ous = new List<ADOrganizationalUnitResponse>();

                try
                {
                    string ldapPath = $"LDAP://{GetDomainPath()}";

                    using var entry = new DirectoryEntry(ldapPath);
                    using var searcher = new DirectorySearcher(entry);

                    searcher.Filter = "(objectClass=organizationalUnit)";
                    searcher.PropertiesToLoad.AddRange(new[] { "name", "distinguishedName", "description" });
                    searcher.PageSize = 1000;
                    searcher.ReferralChasing = ReferralChasingOption.All;
                    searcher.SearchScope = SearchScope.Subtree;

                    var results = searcher.FindAll();
                    
                    foreach (SearchResult result in results)
                    {
                        var ou = new ADOrganizationalUnitResponse
                        {
                            Name = GetPropertyValue(result, "name") ?? string.Empty,
                            DistinguishedName = GetPropertyValue(result, "distinguishedName") ?? string.Empty,
                            Description = GetPropertyValue(result, "description")
                        };
                        
                        ou.ComputerCount = GetComputerCountInOU(ou.DistinguishedName);
                        
                        ous.Add(ou);
                    }
                    
                    _logger.LogInformation("Retrieved {Count} organizational units", ous.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error querying Active Directory for OUs");
                }
                
                return ous;
            });
        }

        public async Task<ADComputerListResponse> GetComputersInOUAsync(string ouPath)
        {
            var request = new ADComputerSearchRequest
            {
                OrganizationalUnit = ouPath,
                EnabledOnly = true
            };
            
            return await GetAllComputersAsync(request);
        }

        public async Task<bool> IsComputerOnlineAsync(string computerName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(computerName))
                        return false;

                    using var ping = new Ping();
                    var reply = ping.Send(computerName, 1000);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<List<ADComputerResponse>> SearchComputersAsync(string searchPattern)
        {
            var request = new ADComputerSearchRequest
            {
                SearchPattern = searchPattern,
                EnabledOnly = false
            };
            
            var result = await GetAllComputersAsync(request);
            return result.Computers;
        }

        private string BuildComputerFilter(ADComputerSearchRequest? request)
        {
            var filters = new List<string> { "(objectClass=computer)" };

            if (request?.EnabledOnly == true)
            {
                filters.Add("(!(userAccountControl:1.2.840.113556.1.4.803:=2))");
            }

            if (!string.IsNullOrWhiteSpace(request?.SearchPattern))
            {
                filters.Add($"(name=*{request.SearchPattern}*)");
            }

            if (!string.IsNullOrWhiteSpace(request?.OperatingSystemFilter))
            {
                filters.Add($"(operatingSystem=*{request.OperatingSystemFilter}*)");
            }

            return filters.Count > 1 ? $"(&{string.Join("", filters)})" : filters[0];
        }

        private void ConfigureSearchProperties(DirectorySearcher searcher)
        {
            searcher.PropertiesToLoad.AddRange(new[]
            {
                "name",
                "cn",
                "dNSHostName",
                "distinguishedName",
                "operatingSystem",
                "operatingSystemVersion",
                "lastLogonTimestamp",
                "description",
                "location",
                "userAccountControl"
            });
        }

        private ADComputerResponse MapToADComputer(SearchResult result)
        {
            var computer = new ADComputerResponse
            {
                Name = GetPropertyValue(result, "name") ?? GetPropertyValue(result, "cn") ?? string.Empty,
                DnsHostName = GetPropertyValue(result, "dNSHostName"),
                DistinguishedName = GetPropertyValue(result, "distinguishedName"),
                OperatingSystem = GetPropertyValue(result, "operatingSystem"),
                OperatingSystemVersion = GetPropertyValue(result, "operatingSystemVersion"),
                Description = GetPropertyValue(result, "description"),
                Location = GetPropertyValue(result, "location"),
                Enabled = !IsAccountDisabled(result)
            };

            var lastLogonStr = GetPropertyValue(result, "lastLogonTimestamp");
            if (!string.IsNullOrEmpty(lastLogonStr) && long.TryParse(lastLogonStr, out long lastLogonTicks))
            {
                computer.LastLogon = DateTime.FromFileTime(lastLogonTicks);
            }

            return computer;
        }

        private bool IsAccountDisabled(SearchResult result)
        {
            var uacStr = GetPropertyValue(result, "userAccountControl");
            if (int.TryParse(uacStr, out int uac))
            {
                const int ACCOUNTDISABLE = 0x0002;
                return (uac & ACCOUNTDISABLE) != 0;
            }
            return false;
        }

        private string? GetPropertyValue(SearchResult result, string propertyName)
        {
            if (result.Properties.Contains(propertyName) && result.Properties[propertyName].Count > 0)
            {
                return result.Properties[propertyName][0]?.ToString();
            }
            return null;
        }

        private string? GetPropertyValue(DirectoryEntry entry, string propertyName)
        {
            if (entry.Properties.Contains(propertyName) && entry.Properties[propertyName].Count > 0)
            {
                return entry.Properties[propertyName][0]?.ToString();
            }
            return null;
        }

        private string GetLdapPath(string? ouPath)
        {
            if (!string.IsNullOrWhiteSpace(ouPath))
            {
                return $"LDAP://{ouPath}";
            }
            return $"LDAP://{GetDomainPath()}";
        }

        private string GetDomainPath()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_domainName))
                {
                    return string.Join(",", _domainName.Split('.').Select(part => $"DC={part}"));
                }

                using var context = new PrincipalContext(ContextType.Domain);
                var domainName = context.ConnectedServer;

                if (!string.IsNullOrWhiteSpace(domainName))
                {
                    var parts = domainName.Split('.');
                    if (parts.Length > 0)
                    {
                        return string.Join(",", parts.Select(part => $"DC={part}"));
                    }
                }

                return "DC=domain,DC=com";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting domain path, using default");
                return "DC=domain,DC=com";
            }
        }

        private int GetComputerCountInOU(string ouPath)
        {
            try
            {
                using var entry = new DirectoryEntry($"LDAP://{ouPath}");
                using var searcher = new DirectorySearcher(entry);

                searcher.Filter = "(objectClass=computer)";
                searcher.PropertiesToLoad.Add("cn");
                searcher.ReferralChasing = ReferralChasingOption.All;

                var results = searcher.FindAll();
                return results.Count;
            }
            catch
            {
                return 0;
            }
        }

        private ADComputerListResponse GetComputersUsingPrincipalContext(ADComputerSearchRequest? request)
        {
            var response = new ADComputerListResponse();

            try
            {
                using var context = new PrincipalContext(ContextType.Domain, _domainName);
                using var computerPrincipal = new ComputerPrincipal(context);

                if (!string.IsNullOrWhiteSpace(request?.SearchPattern))
                {
                    computerPrincipal.Name = $"*{request.SearchPattern}*";
                }

                using var searcher = new PrincipalSearcher(computerPrincipal);

                foreach (var result in searcher.FindAll())
                {
                    if (result is ComputerPrincipal computer)
                    {
                        try
                        {
                            if (request?.EnabledOnly == true && computer.Enabled == false)
                                continue;

                            var adComputer = new ADComputerResponse
                            {
                                Name = computer.Name ?? string.Empty,
                                DistinguishedName = computer.DistinguishedName,
                                Enabled = computer.Enabled ?? false,
                                LastLogon = computer.LastLogon,
                                Description = computer.Description
                            };

                            var directoryEntry = computer.GetUnderlyingObject() as DirectoryEntry;
                            if (directoryEntry != null)
                            {
                                adComputer.DnsHostName = GetPropertyValue(directoryEntry, "dNSHostName");
                                adComputer.OperatingSystem = GetPropertyValue(directoryEntry, "operatingSystem");
                                adComputer.OperatingSystemVersion = GetPropertyValue(directoryEntry, "operatingSystemVersion");
                                adComputer.Location = GetPropertyValue(directoryEntry, "location");

                                if (!string.IsNullOrWhiteSpace(request?.OperatingSystemFilter) &&
                                    (adComputer.OperatingSystem == null || 
                                     !adComputer.OperatingSystem.Contains(request.OperatingSystemFilter, StringComparison.OrdinalIgnoreCase)))
                                {
                                    continue;
                                }
                            }

                            if (request?.CheckOnlineStatus == true)
                            {
                                adComputer.IsOnline = IsComputerOnlineAsync(adComputer.DnsHostName ?? adComputer.Name).Result;
                            }

                            response.Computers.Add(adComputer);

                            if (adComputer.Enabled)
                                response.EnabledCount++;

                            if (adComputer.IsOnline)
                                response.OnlineCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error processing computer: {Name}", computer.Name);
                        }
                    }
                }

                response.TotalCount = response.Computers.Count;
                _logger.LogInformation("Retrieved {Count} computers using PrincipalContext", response.TotalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying computers using PrincipalContext");
                throw;
            }

            return response;
        }
    }
}
