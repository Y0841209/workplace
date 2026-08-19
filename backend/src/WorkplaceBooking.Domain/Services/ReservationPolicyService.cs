using Ardalis.Specification;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;
using WorkplaceBooking.SharedKernel.Primitives;

namespace WorkplaceBooking.Domain.Services;

public class ReservationPolicyService : IReservationPolicyService
{
    private readonly IRepository<AppSettings> _settingsRepository;
    private readonly IRepository<ReservationException> _exceptionRepository;
    private readonly IRepository<UserApplicationRole> _userRoleRepository;
    private readonly IRepository<UserBusinessProfile> _userProfileRepository;
    private readonly IRepository<ResourceAccessPolicy> _policyRepository;

    public ReservationPolicyService(
        IRepository<AppSettings> settingsRepository,
        IRepository<ReservationException> exceptionRepository,
        IRepository<UserApplicationRole> userRoleRepository,
        IRepository<UserBusinessProfile> userProfileRepository,
        IRepository<ResourceAccessPolicy> policyRepository)
    {
        _settingsRepository = settingsRepository;
        _exceptionRepository = exceptionRepository;
        _userRoleRepository = userRoleRepository;
        _userProfileRepository = userProfileRepository;
        _policyRepository = policyRepository;
    }

    public async Task<int> GetMaxFutureReservationsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.FirstOrDefaultAsync(new SingleAppSettingsSpec(), cancellationToken);
        return settings?.MaximumFutureActiveReservations ?? 5;
    }

    public async Task<bool> HasActiveExceptionAsync(Guid userId, string? resourceTypeCode, CancellationToken cancellationToken = default)
    {
        var spec = new ActiveExceptionForUserSpec(userId, resourceTypeCode);
        var exception = await _exceptionRepository.FirstOrDefaultAsync(spec, cancellationToken);
        return exception != null && exception.IsActiveOn(DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<bool> CanReserveAsync(Guid userId, string resourceTypeCode, CancellationToken cancellationToken = default)
    {
        // GLOBAL_ADMIN can always reserve
        var hasGlobalAdmin = await _userRoleRepository.AnyAsync(
            new ActiveRoleForUserSpec(userId, "GLOBAL_ADMIN"), cancellationToken);
        if (hasGlobalAdmin) return true;

        // Check user's business profiles against policies
        var profiles = await _userProfileRepository.ListAsync(new ActiveProfilesForUserSpec(userId), cancellationToken);
        foreach (var profile in profiles)
        {
            var policy = await _policyRepository.FirstOrDefaultAsync(
                new PolicyForProfileAndTypeSpec(profile.ProfileCode, resourceTypeCode), cancellationToken);
            if (policy != null && policy.Active && policy.CanReserve)
                return true;
        }
        return false;
    }
}