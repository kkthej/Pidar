using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pidar.Models;
using System.Diagnostics;

namespace Pidar.Controllers
{
    /// <summary>
    /// Error pages shown to visitors. They never show exception details (those go to the log):
    /// the message on the page is generic, and the Request ID lets you find the entry in the log.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        // --------------------------------------------------------------------
        // HANDLE STATUS CODE ERRORS (404, 500, etc.)
        // --------------------------------------------------------------------
        [Route("Error/{statusCode:int}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            switch (statusCode)
            {
                case 404:
                    model.ErrorMessage = "Sorry, the page or resource you requested could not be found.";
                    break;

                case 500:
                    model.ErrorMessage = "Internal server error. Something went wrong on the server.";
                    break;

                default:
                    model.ErrorMessage = $"An error occurred (Status Code: {statusCode}).";
                    break;
            }

            Response.StatusCode = statusCode;
            return View("Error", model);
        }

        // --------------------------------------------------------------------
        // HANDLE UNHANDLED EXCEPTIONS (global errors, via UseExceptionHandler("/Error"))
        // --------------------------------------------------------------------
        [Route("Error")]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature?.Error != null)
            {
                _logger.LogError(exceptionFeature.Error,
                    "Unhandled exception on {Path} (Request ID {RequestId})", exceptionFeature.Path, requestId);
            }

            var model = new ErrorViewModel
            {
                RequestId = requestId,
                ErrorMessage = "An unexpected error occurred. Please try again later."
            };

            return View("Error", model);
        }
    }
}
