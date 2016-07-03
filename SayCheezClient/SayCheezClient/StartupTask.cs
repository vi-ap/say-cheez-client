using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

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

        private async void initialize()
        {
            Debug.WriteLine("Beginning of initialize");
            string photosFolderName = "SayCheez";
            
            IStorageItem folder = await KnownFolders.SavedPictures.TryGetItemAsync(photosFolderName);

            photosFolder = folder != null ? (StorageFolder)folder : await KnownFolders.SavedPictures.CreateFolderAsync(photosFolderName);
            Debug.WriteLine("Line after creating/retrieving photos folder. " + photosFolder.Path);

        }

        private async void takePhoto()
        {            
            StorageFile photoFile;

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            Debug.WriteLine("Just before creating photo file");
            photoFile = await photosFolder.CreateFileAsync(String.Format("{0:dd-MM-yyyy HH-mm-ss}.jpg", DateTime.Now), CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), photoFile);

            Debug.WriteLine("Picture taken" + photoFile.Path);

            HttpClient httpClient = new HttpClient();
            try
            {
                IBuffer bufferContent = await FileIO.ReadBufferAsync(photoFile);
                string json = JsonConvert.SerializeObject(new { Time = DateTime.Now, Content = WindowsRuntimeBufferExtensions.ToArray(bufferContent) });
                Debug.WriteLine(json);
                var result = await httpClient.PostAsync(new Uri("http://saycheez.azurewebsites.net/api/pictures/"), new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
                Debug.WriteLine(result);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.InnerException);
            }
            httpClient.Dispose();
        }
    }
}
