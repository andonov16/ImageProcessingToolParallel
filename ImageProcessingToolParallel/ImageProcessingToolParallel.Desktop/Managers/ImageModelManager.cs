namespace ImageProcessingToolParallel.Desktop.Managers
{
    public abstract class ImageModelManager
    {
        public int BatchSize { get; }



        public ImageModelManager()
        {
            this.BatchSize = int.Parse(App.AppConfiguration["BatchSize"]);
        }

        public ImageModelManager(int batchSize)
        {
            this.BatchSize = batchSize;
        }
    }
}
