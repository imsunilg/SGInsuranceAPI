using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGInsurance.Application.Services;

namespace SGInsurance.Api.Controllers;

[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationQueryService _notificationQuery;

    public NotificationsController(INotificationQueryService notificationQuery)
    {
        _notificationQuery = notificationQuery;
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<ActionResult> GetByCustomer(Guid customerId) => Ok(await _notificationQuery.GetByCustomerAsync(customerId));
}
