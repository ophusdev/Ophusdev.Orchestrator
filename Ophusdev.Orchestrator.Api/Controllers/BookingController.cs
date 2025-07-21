using Microsoft.AspNetCore.Mvc;
using Ophusdev.Orchestrator.Business.Abstraction;
using Ophusdev.Orchestrator.Shared;
using Ophusdev.Orchestrator.Shared.Exceptions;

namespace Ophusdev.Orchestrator.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BookingController : ControllerBase
    {
        private readonly ILogger<BookingController> _logger;
        private readonly IBusiness _business;

        public BookingController(IBusiness business, ILogger<BookingController> logger)
        {
            _logger = logger;
            _business = business;
        }

        [HttpPost(Name = "BookRoom")]
        public async Task<ActionResult> BookRoom(BookingInsertDto bookingDto)
        {
            if (bookingDto.RoomId <= 0)
            {
                return BadRequest("RoomId must be greater than 0.");
            }

            if (bookingDto.GuestId <= 0)
            {
                return BadRequest("GuestId must be greater than 0.");
            }

            try
            {
                BookingResponse response = await _business.CreateBookingSaga(bookingDto);

                return new JsonResult(response);
            }
            catch (RoomNotFoundException)
            {
                return BadRequest("RoomId not found");
            }
        }

        [HttpGet(Name = "Status")]
        public async Task<ActionResult> Status(string bookingId)
        {
            BookingResponse response = await _business.GetBookingStatus(bookingId);

            return new JsonResult(response);
        }
    }
}
