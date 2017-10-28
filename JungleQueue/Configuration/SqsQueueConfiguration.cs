using Amazon;

namespace JungleQueue.Configuration
{
    public class SqsQueueConfiguration : QueueConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the input queue
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the AWS Region
        /// </summary>
        public RegionEndpoint Region { get; set; }
    }
}
