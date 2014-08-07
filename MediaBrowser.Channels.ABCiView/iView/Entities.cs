using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Channels.ABCiView.iViewEntities
{

    public class ConfigInfo
    {
        public string ApiUrl { get; set; }
        public string AuthUrl { get; set; }
        public string CategoriesUrl { get; set; }
        public string ServerStreamngUrl { get; set; }
        public string ServerFallbackUrl { get; set; }
    }
    public class AuthInfo
    {
        public string Token { get; set; }
        public string TokenHD { get; set; }
        public string Server { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }
    }
    public class RootCategories : BaseModel
    {
        public List<Category> Categories;
    }
    public class Category : BaseModel
    {
        public Category()
            : base()
        {
            //
        }
        public string id { get; set; }
        public string name { get; set; }
        public bool? index { get; set; }
        public bool? genre { get; set; }
        public List<Category> Categories;
    }
    public class SeriesIndex : BaseSeries
    {
        public SeriesIndex()
            : base()
        {
            //
        }

        /// <summary>
        /// array of JSON program objects
        /// </summary>
        public List<ProgramIndex> f { get; set; }

        public int ProgramCount { get { return f == null ? 0 : f.Count(); } }
    }
    public class Series : BaseSeries
    {
        public Series()
            : base()
        {
            //
        }

        /// <summary>
        /// Series description; also program description for programs with only one episode
        /// </summary>
        public string c { get; set; }
        /// <summary>
        /// URL of image to associate with series
        /// </summary>
        public string d { get; set; }

        /// <summary>
        /// array of JSON program objects
        /// </summary>
        public List<Program> f { get; set; }

        public int ProgramCount { get { return f == null ? 0 : f.Count(); } }
    }
    public abstract class BaseSeries : BaseModel
    {
        public BaseSeries()
            : base()
        {
            //
        }

        /// <summary>
        /// 7-digit series identifer
        /// </summary>
        public string a { get; set; }
        /// <summary>
        /// Series name
        /// </summary>
        public string b { get; set; }
        /// <summary>
        /// keywords
        /// </summary>
        public string e { get; set; }
    }

    public class ProgramIndex : BaseProgram
    {
        public ProgramIndex()
            : base()
        {
            //
        }
    }
    public class Program : BaseProgram
    {
        public Program()
            : base()
        {
            //
        }

        /// <summary>
        /// Program name
        /// </summary>
        public string b { get; set; }
        /// <summary>
        /// Program description
        /// </summary>
        public string d { get; set; }
        /// <summary>
        /// Program category
        /// </summary>
        public string e { get; set; }

        /// <summary>
        /// Transmission date/time
        /// </summary>
        public string h { get; set; }
        /// <summary>
        /// Download size in MB
        /// </summary>
        public string i { get; set; }
        /// <summary>
        /// Program length in seconds
        /// </summary>
        public string j { get; set; }
        /// <summary>
        /// Text to associate with the next tag ('l')
        /// </summary>
        public string k { get; set; }
        /// <summary>
        /// URL associated with program
        /// </summary>
        public string l { get; set; }
        /// <summary>
        /// Show classification rating
        /// </summary>
        public string m { get; set; }
        /// <summary>
        /// filename to download
        /// </summary>
        public string n { get; set; }
        /// <summary>
        /// classification reasons
        /// </summary>
        public string o { get; set; }
        /// <summary>
        /// RTMP stream URL
        /// </summary>
        public string r { get; set; }
        /// <summary>
        /// URL of image to associate with program
        /// </summary>
        public string s { get; set; }
        /// <summary>
        /// "1" for News 24 streams - meaning uncertain
        /// </summary>
        public string t { get; set; }
        /// <summary>
        /// Series number for program. "1", where present for un-numbered series
        /// </summary>
        public string u { get; set; }
        /// <summary>
        /// Episode number for program
        /// </summary>
        public string v { get; set; }
        /// <summary>
        /// Channel, day and time when program is broadcast
        /// </summary>
        public string w { get; set; }
    }
    public class BaseProgram : BaseModel
    {
        public BaseProgram()
            : base()
        {
            //
        }

        /// <summary>
        /// 6-digit program identifier
        /// </summary>
        public string a { get; set; }
        /// <summary>
        /// When program made available on iView (date/time)
        /// </summary>
        public string f { get; set; }
        /// <summary>
        /// iView program expiry (date/time)
        /// </summary>
        public string g { get; set; }
    }

    public class BaseModel
    {
        public BaseModel()
            : base()
        {

        }
    }
}
