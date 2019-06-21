namespace MyFunctionApp.Services
{
    public class MyServiceB
    {
        public MyServiceB(ICommonIdProvider idProvider)
        {
            IdProvider = idProvider;
        }

        public ICommonIdProvider IdProvider { get; }
    }
}
