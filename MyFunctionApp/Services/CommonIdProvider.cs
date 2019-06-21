using System;

namespace MyFunctionApp.Services
{
    public class CommonIdProvider : IGlobalIdProvider
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}
