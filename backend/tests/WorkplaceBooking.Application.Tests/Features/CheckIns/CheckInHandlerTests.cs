using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Application.Features.Reservations.Handlers;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.Tests.Features.CheckIns;

public class CheckInHandlerTests
{
    // TODO: Implement tests for CheckInReservationHandler
    // Test cases:
    // - Should_Check_In_When_Valid_QR_And_Owner
    // - Should_Fail_When_Reservation_Not_Found
    // - Should_Fail_When_Not_Owner
    // - Should_Fail_When_Status_Not_Confirmed
    // - Should_Fail_When_Resource_Type_Not_Office
    // - Should_Fail_When_QR_Does_Not_Match
    // - Should_Fail_When_Not_Today
    // - Should_Fail_When_Outside_Time_Window
    // - Should_Update_Reservation_Status_To_Checked_In
}