using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pidar.Models;
using System.Diagnostics;

namespace Pidar.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        // --------------------------------------------------------------------
        // HANDLE STATUS CODE ERRORS (404, 500, etc.)
        // --------------------------------------------------------------------
        [Route("Error/{statusCode}")]
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

            return View("Error", model);
        }

        // --------------------------------------------------------------------
        // HANDLE UNHANDLED EXCEPTIONS (global errors)
        // --------------------------------------------------------------------
        [Route("Error")]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = "An unexpected error occurred."
            };

            // Attempt to retrieve the real exception
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                // Log the real error (critical during development)
                model.ErrorMessage = exceptionFeature.Error.Message;

                // You could also log the path:
                // exceptionFeature.Path
            }

            return View("Error", model);
        }
    }
}
