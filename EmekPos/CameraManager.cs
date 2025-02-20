using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using ZXing.Net.Maui.Controls;

namespace ZXing.Net.Maui
{
    internal partial class CameraManager : IDisposable
    {
        public CameraManager(IMauiContext context, CameraLocation cameraLocation)
        {
            Context = context;
            CameraLocation = cameraLocation;
        }
        public void Dispose()
        {

        }
        public void UpdateCamera()
        {
            // Kamerayı güncellemek için gerekli işlemler
            // Örneğin, kamera konumunu değiştirme veya yeni bir kamera başlatma işlemi
            Console.WriteLine($"Kamera {CameraLocation} konumunda güncellendi.");
        }
        protected readonly IMauiContext Context;
        public event EventHandler<CameraFrameBufferEventArgs> FrameReady;

        public CameraLocation CameraLocation { get; private set; }

        public void UpdateCameraLocation(CameraLocation cameraLocation)
        {
            CameraLocation = cameraLocation;

            UpdateCamera();
        }
        public void CloseCamera()
        {
            // Kamerayı kapatma işlemleri
            Console.WriteLine("Kamera kapatıldı.");
        }
        public void StartCamera()
        {
            // Kamerayı kapatma işlemleri
            Console.WriteLine("Kamera Açıldı.");
        }
        public async Task<bool> CheckPermissions()
            => (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;
    }
}