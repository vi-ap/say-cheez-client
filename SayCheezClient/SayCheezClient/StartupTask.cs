using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace SayCheezClient
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        MediaCapture mediaCapture;
        StorageFolder photosFolder;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            initialize();
            for (int i=0; i<20; i++)
            {
                Debug.WriteLine("Hi from Run method # " + i);
                try
                {
                    takePhoto();
                }
                catch(Exception ex)
                {
                    Debug.WriteLine("In catch block");
                    Debug.WriteLine(ex.Message);
                }
                Debug.WriteLine("At end of try/catch");
            }
        }

        private async void initialize()
        {
            Debug.WriteLine("Beginning of initialize");
            string photosFolderName = "SayCheez";

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            IStorageItem folder = await KnownFolders.PicturesLibrary.TryGetItemAsync(photosFolderName);

            photosFolder = folder != null ? (StorageFolder)folder : await KnownFolders.PicturesLibrary.CreateFolderAsync(photosFolderName);
            Debug.WriteLine("Line after creating photos folder. " + photosFolder.Path);

        }

        private async void takePhoto()
        {
            
            StorageFile photoFile;

            Debug.WriteLine("Just before creating photo file");
            
            photoFile = await photosFolder.CreateFileAsync(String.Format("{0:dd-MM-yyyy HH-mm-ss}.jpg", DateTime.Now), CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), photoFile);

            Debug.WriteLine("Picture taken" + photoFile.Path);
        }
    }
}
