namespace ImageProcessingToolParallel.Desktop.Managers
{
    public abstract class ImageModelManager
    {
        public int BatchSize { get; }



        public ImageModelManager()
        {
            this.BatchSize = 32;
        }

        public ImageModelManager(int batchSize)
        {
            this.BatchSize = batchSize;
        }
    }
}
