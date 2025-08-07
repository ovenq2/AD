using System.Collections.Generic;
using System.Threading.Tasks;
using ADUserManagement.Models.ViewModels;

namespace ADUserManagement.Services.Interfaces
{
    public interface INetworkService
    {
        // DNS İşlemleri
        Task<int> CreateDnsRequestAsync(DnsRequestViewModel model, string requestedByUsername);
        Task<bool> ApproveDnsRequestAsync(int requestId, string approvedByUsername);
        Task<bool> RejectDnsRequestAsync(int requestId, string rejectionReason, string rejectedByUsername);

        // DHCP İşlemleri  
        Task<int> CreateDhcpReservationRequestAsync(DhcpReservationViewModel model, string requestedByUsername);
        Task<bool> ApproveDhcpReservationRequestAsync(int requestId, string approvedByUsername);
        Task<bool> RejectDhcpReservationRequestAsync(int requestId, string rejectionReason, string rejectedByUsername);

        // DNS/DHCP Gerçek İşlemler (PowerShell veya API çağrıları)
        Task<bool> CreateDnsRecordAsync(string name, string type, string value);
        Task<bool> CreateDhcpReservationAsync(string deviceName, string macAddress, string ipAddress);
    }
}