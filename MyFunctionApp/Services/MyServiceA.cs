namespace MyFunctionApp.Services
{
    public class MyServiceA
    {
        public MyServiceA(ICommonIdProvider idProvider)
        {
            IdProvider = idProvider;
        }

        public ICommonIdProvider IdProvider { get; }
    }
}
