using AsyncImageLoader.Loaders;
using Avalonia.Media.Imaging;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace EnterpriseMessengerUI
{
    public class UpdatableDiskCachedWebImageLoader : DiskCachedWebImageLoader
    {
        public bool Reload = false;

        private readonly ConcurrentDictionary<string, Task<IBitmap?>> _memoryCache = new();

        public UpdatableDiskCachedWebImageLoader(string cacheFolder = "Cache/Images/") : base(cacheFolder) { }

        public UpdatableDiskCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient, string cacheFolder = "Cache/Images/") : base(httpClient, disposeHttpClient, cacheFolder) { }

        public override async Task<IBitmap?> ProvideImageAsync(string url)
        {
            if (Reload) _memoryCache.TryRemove(url, out _);
            var bitmap = await _memoryCache.GetOrAdd(url, LoadAsync);
            if (bitmap == null) _memoryCache.TryRemove(url, out _);
            return bitmap;
        }

        protected override Task<Bitmap?> LoadFromGlobalCache(string url)
        {
            if (Reload)
            {
                Reload = false;
                return base.LoadFromGlobalCache(string.Empty);
            }
            else
            {
                return base.LoadFromGlobalCache(url);
            }
        }
    }
}
