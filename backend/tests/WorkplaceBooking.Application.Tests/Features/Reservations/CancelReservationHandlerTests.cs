using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Application.Features.Reservations.Handlers;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.Tests.Features.Reservations;

public class CancelReservationHandlerTests
{
    // TODO: Implement tests for CancelReservationHandler
    // Test cases:
    // - Should_Cancel_When_Owner
    // - Should_Cancel_When_Support
    // - Should_Fail_When_Not_Owner_And_Not_Support
    // - Should_Fail_When_Already_Cancelled
    // - Should_Fail_When_Completed
    // - Should_Require_Reason_For_Support
}