namespace MQTTnet.Internal
{
    public class AsyncQueueDequeueResult<TItem>
    {
        public AsyncQueueDequeueResult(bool isSuccess, TItem item)
        {
            IsSuccess = isSuccess;
            Item = item;
        }

        public bool IsSuccess { get; }

        public TItem Item { get; }
    }
}