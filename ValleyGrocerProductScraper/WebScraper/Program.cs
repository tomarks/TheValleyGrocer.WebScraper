using AngleSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace WebScraper
{
    class Program
    {
        public static readonly string baseUrl = "https://www.thevalleygrocer.com.au/shop?page=200";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            HttpClient client = new HttpClient();

            var result =  client.GetAsync(baseUrl).Result;
            result.EnsureSuccessStatusCode();
            var html = result.Content.ReadAsStringAsync().Result;

            var context = new BrowsingContext(Configuration.Default);
            var document = context.OpenAsync(req => req.Content(html)).Result;
            var rootProducts = document.All.Where(el => el.Attributes.Select(atr => atr.Value).Contains("product-item-root")).ToList();

            var products = new Products();

            if(!Directory.Exists("Images"))
                Directory.CreateDirectory("Images");

            using WebClient webClient = new WebClient();

            for (int i = 0; i < rootProducts.Count; i++)
            {
                var el = rootProducts[i];

                var productLink = el.QuerySelectorAll("a").First().Attributes.Where(x => x.LocalName == "href").First().Value;
                var uri = new Uri(productLink);
                var productName = el.QuerySelectorAll("h3").First().TextContent;
                var elHtml = el.QuerySelectorAll("a").FirstOrDefault().OuterHtml;
                var re = Regex.Match(elHtml, @"(https:\/\/static.wixstatic.com.*.[jpg|jpeg])(?:\/)");
                var imageUrl = re.Groups[0].Value;
                if (re.Groups.Count > 1)
                    imageUrl = re.Groups[1].Value;

                FileInfo fi = new FileInfo(imageUrl);
                var sku = uri.Segments[uri.Segments.Length - 1];

                webClient.DownloadFile(imageUrl, $"Images/{sku}{fi.Extension}");

                //<span data-hook="product-item-price-to-pay" class="_23ArP">$1.50</span>
                var price = el.QuerySelectorAll("span[data-hook='product-item-price-to-pay']").First().TextContent;
                // Get first <a> tag and store the href into ProductURL
                //Store into product name - <h3 class="_2BULo" data-hook="product-item-name">Thai Noodle Sauce</h3> 
                products.List.Add(new Product()
                {
                    Id = i,
                    Name = productName,
                    ImageURL = imageUrl,
                    Price = price,
                    ProductURL = productLink,
                    SKU= sku
                });
            }


            var jsonString = JsonConvert.SerializeObject(products, Formatting.Indented);
            File.WriteAllText("Output.json", jsonString);

            Console.ReadKey();

        }
    }

    public class Products
    {
        public List<Product> List { get; set; } = new List<Product>();
    }

    public class Product
    {
        public int Id { get; set; }
        public string SKU { get; set; }

        public string Name { get; set; }
        public string Price { get; set; }
        public string ImageURL { get; set; }
        public string ProductURL { get; set; }
        public string Weight { get; set; }
    }
}
