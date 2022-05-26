using Kentico.Kontent.Delivery.Abstractions;

namespace DeliverySDKWithAutofac
{
	public class Article
    {
        public string Title { get; set; }
        public string Text { get; set; }

        public List<Writer> Writers { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }

    public class Writer
    {
        public string Fullname { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }

    public class Movie
    {
        public string Title { get; set; }
        public string Plot { get; set; }
        public List<Actor> Stars { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }

    public class Actor
    {
        public string Title { get; set; }
        public string FirstName { get; set; }
        public IContentItemSystemAttributes System { get; set; }
    }
}
