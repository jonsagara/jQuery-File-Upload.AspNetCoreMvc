namespace jQuery_File_Upload.AspNetCoreMvc.Models
{
    public class ViewDataUploadFilesResult
    {
        public string name { get; set; }
        public long size { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string deleteUrl { get; set; }
        public string thumbnailUrl { get; set; }
        public string deleteType { get; set; }
    }
}
