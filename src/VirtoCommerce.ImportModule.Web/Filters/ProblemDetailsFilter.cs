using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace VirtoCommerce.ImportModule.Web.Filters
{
    /// <summary>
    /// Translates <see cref="ValidationException"/> thrown inside controller actions into a
    /// ValidationProblemDetails 400 response -- the same shape <see cref="ApiControllerAttribute"/>
    /// produces for binding-time validation. Without this filter the platform's
    /// ApiErrorWrappingMiddleware would swallow the exception as 500.
    ///
    /// Apply per-controller with [TypeFilter(typeof(ProblemDetailsFilter))].
    /// </summary>
    public class ProblemDetailsFilter : IResultFilter, IExceptionFilter
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;

        public ProblemDetailsFilter(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
        {
            _apiBehaviorOptions = apiBehaviorOptions.Value;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ObjectResult { Value: ProblemDetails problemDetails })
            {
                SetMessage(problemDetails);
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not ValidationException validationException)
            {
                return;
            }

            foreach (var error in validationException.Errors)
            {
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            var result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);

            if (result is ObjectResult { Value: ValidationProblemDetails problemDetails })
            {
                SetMessage(problemDetails);
            }

            context.Result = result;
            context.ExceptionHandled = true;
        }

        private static void SetMessage(ProblemDetails problemDetails)
        {
            var message = problemDetails.Detail;
            if (problemDetails is ValidationProblemDetails validationProblemDetails)
            {
                var errors = validationProblemDetails.Errors
                    .SelectMany(x => x.Value)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();
                if (errors.Length > 0)
                {
                    message = string.Join('\n', errors);
                }
            }

            // Platform admin UI reads Extensions["message"] for error display.
            problemDetails.Extensions["message"] = message;
        }
    }
}
