using System.Net;
using System.Text.Json;

public class ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> _logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // بنقول للريكويست يكمل طريقه عادي للـ Controllers والـ Services
            await _next(context);
        }
        catch (Exception ex)
        {
            // لو أي كود ضرب Exception في أي مكان، هيقع هنا
            _logger.LogError(ex, "Un-Expected Error occur!");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        int statusCode = (int)HttpStatusCode.InternalServerError;
        string message = "Un-Expected Error occur, please try again later";
        string title = "Internal Server Error";

        switch (exception)
        {
            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized; // 401
                title = "Unauthorized";
                message = exception.Message;
                break;

            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound; // 404
                title = "Not Found";
                message =exception.Message;
                break;

                // تقدري تضيفي أي Exceptions تانية هنا
        }

        context.Response.StatusCode = statusCode;

        var errorResponse = new
        {
            Title = title,
            StatusCode = statusCode,
            Message = message,
            // Detailed = exception.Message 
        };

        var result = JsonSerializer.Serialize(errorResponse);
        return context.Response.WriteAsync(result);
    }
}