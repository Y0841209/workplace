using System.Threading.Tasks;

namespace WorkplaceBooking.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}