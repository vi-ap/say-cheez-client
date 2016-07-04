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
        private const string serviceUri = "http://saycheez.azurewebsites.net/api/uploads/";

        BackgroundTaskDeferral deferral;
        MediaCapture mediaCapture;
        StorageFolder picturesFolder;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();

            initializePicturesFolder();
            try
            {
                runPictureCaptureTask();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
        }

        private async void initializePicturesFolder()
        {
            string photosFolderName = "SayCheez";            
            IStorageItem folder = await KnownFolders.SavedPictures.TryGetItemAsync(photosFolderName);
            picturesFolder = folder != null ? (StorageFolder)folder : await KnownFolders.SavedPictures.CreateFolderAsync(photosFolderName);

        }

        private async void captureAndSendPicture()
        {            
            StorageFile photoFile;

            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            DateTime timeNow = DateTime.Now;
            photoFile = await picturesFolder.CreateFileAsync(String.Format("{0:dd-MM-yyyy HH-mm-ss}.jpg", timeNow), CreationCollisionOption.GenerateUniqueName);
            await mediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), photoFile);

            Debug.WriteLine("Picture taken" + photoFile.Path);

            HttpClient httpClient = new HttpClient();
            IBuffer bufferContent = await FileIO.ReadBufferAsync(photoFile);
            string json = JsonConvert.SerializeObject(new { Time = timeNow, SerializedContent = Convert.ToBase64String(WindowsRuntimeBufferExtensions.ToArray(bufferContent)) });
            Debug.WriteLine(json);
            var result = await httpClient.PostAsync(new Uri(serviceUri), new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
            Debug.WriteLine(result);

            httpClient.Dispose();
        }

        private void runPictureCaptureTask()
        {
            var delayTask = Task.Run(async () =>
            {
                await Task.Delay(10000);
                captureAndSendPicture();
            });

            delayTask.Wait();
            runPictureCaptureTask();
        }
    }
}
