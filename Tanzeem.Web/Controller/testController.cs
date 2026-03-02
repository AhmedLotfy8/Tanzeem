using Microsoft.AspNetCore.Mvc;

namespace Tanzeem.Web.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase {
        // GET api/test/hello
        [HttpGet("hello")]
        public IActionResult Hello() {
            return Ok("Hello from TestController!");
        }

        // GET api/test/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(int id) {
            return Ok($"You requested item with ID = {id}");
        }
    }
}
