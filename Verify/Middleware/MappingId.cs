namespace MACUTION.Middleware
{
    public class MappingId
    {
        private readonly RequestDelegate _next;
        public MappingId (RequestDelegate next)
        {
            this._next=next;
        }
        public async Task Invoke(HttpContext context)
        {
          var Id = context.User.Claims.Where(x => x.Type == "ID").FirstOrDefault()?.Value;
          Console.WriteLine(Id);
          context.Items["id"]=Id;
          await  _next(context);
        } 
     }
}