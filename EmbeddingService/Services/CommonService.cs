using BOEmbeddingService.Interfaces;

namespace BOEmbeddingService.Services
{
    public class CommonService : ICommonService
	{
		public async Task<string[]> GetFiles(string path)
		{
            List<string> files = new List<string>();
            try
			{
				foreach (string f in Directory.GetFiles(path))
				{
					files.Add(f);
				}
				foreach (string d in Directory.GetDirectories(path))
				{
					if (!d.Contains("bin") && !d.Contains("obj"))
					{
						GetFiles(d);
					}
				}
			}
			catch (System.Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return files.ToArray();
		}
	}
}
