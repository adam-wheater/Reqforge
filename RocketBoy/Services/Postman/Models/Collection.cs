namespace RocketBoy.Services.Postman.Models
{
    public class Collection
    {
        public Info info { get; set; }
        public List<Variable> variable { get; set; }
        public List<Event> @event { get; set; }
        public List<Item> item { get; set; }
        public ProtocolProfileBehavior protocolProfileBehavior { get; set; }
    }

    public class Auth
    {
        public string type { get; set; }
        public Basic basic { get; set; }
        public Hawk hawk { get; set; }
    }

    public class Basic
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class Cookie
    {
        public string domain { get; set; }
        public int expires { get; set; }
        public bool hostOnly { get; set; }
        public bool httpOnly { get; set; }
        public string key { get; set; }
        public string path { get; set; }
        public bool secure { get; set; }
        public bool session { get; set; }
        public string _postman_storeId { get; set; }
        public string value { get; set; }
    }

    public class Description
    {
        public string content { get; set; }
        public string version { get; set; }
        public string type { get; set; }
    }

    public class Event
    {
        public string listen { get; set; }
        public string id { get; set; }
        public Script script { get; set; }
    }

    public class Hawk
    {
        public string authKey { get; set; }
    }

    public class Header
    {
        public string key { get; set; }
        public string value { get; set; }
        public string description { get; set; }
    }

    public class Info
    {
        public string name { get; set; }
        public string id { get; set; }
        public string _postman_schema { get; set; }
        public Version version { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public Description description { get; set; }
        public string name { get; set; }
        public object request { get; set; }
        public List<Response> response { get; set; }
        public List<Event> @event { get; set; }
        public Proxy proxy { get; set; }
        public ProtocolProfileBehavior protocolProfileBehavior { get; set; }
        public string _my_meta { get; set; }
        public List<Item> item { get; set; }
    }

    public class Package1
    {
        public string id { get; set; }
    }

    public class Packages
    {
        public Package1 package1 { get; set; }
    }

    public class ProtocolProfileBehavior
    {
        public bool disableBodyPruning { get; set; }
    }

    public class Proxy
    {
        public string match { get; set; }
        public string server { get; set; }
    }

    public class Request
    {
        public Description description { get; set; }
        public Url url { get; set; }
        public List<Header> header { get; set; }
        public Auth auth { get; set; }
    }

    public class Response
    {
        public string name { get; set; }
        public string originalRequest { get; set; }
        public string status { get; set; }
        public int code { get; set; }
        public string header { get; set; }
        public List<Cookie> cookie { get; set; }
        public string body { get; set; }
    }

    public class Script
    {
        public string type { get; set; }
        public List<string> exec { get; set; }
        public Packages packages { get; set; }
    }

    public class Url
    {
        public string description { get; set; }
        public string protocol { get; set; }
        public string port { get; set; }
        public string path { get; set; }
        public string host { get; set; }
    }

    public class Variable
    {
        public string id { get; set; }
        public string type { get; set; }
        public string value { get; set; }
    }

    public class Version
    {
        public string major { get; set; }
        public string minor { get; set; }
        public string patch { get; set; }
        public string prerelease { get; set; }
    }
}