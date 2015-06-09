using System.Collections.Specialized;

namespace ImageOptimizerV2.ImageOptimizer
{
    public class OptimizeImageManagerStatus
    {
        public OptimizeImageManagerStatusEnum Status { get; set; }

        public int AlreadyOptimized { get; set; }
        public int Skipped { get; set; }

        public StringCollection Messages { get; set; }

        public OptimizeImageManagerStatus()
        {
            Messages = new StringCollection();
            Status = OptimizeImageManagerStatusEnum.Initializing;            
        }
    }

    public enum OptimizeImageManagerStatusEnum
    {
        Failed,
        Running,
        Finished,
        Initializing
    }
}
