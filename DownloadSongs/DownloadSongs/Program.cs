using System;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml;

namespace DownloadSongs
{
    class Program
    {
        

        static void Main(string[] args)
        {
            
            Console.WriteLine("Batch download songs from ApunkaBollywood.com");
            Console.WriteLine();

            string url = string.Empty;
            string body = string.Empty;
            string bodyNoWhiteSpace = string.Empty;

            List<string> movieList = new List<string>();
            List<MovieSongs> movieSonglist = new List<MovieSongs>();

            try
            {

                Logger("Batch download started...");
                Logger("");

                //Set the root URL
                url = @"http://www.apunkabollywood.us/browser/category/view/9114/2019";

                Logger("Main URL: " + url);

                //Get the list of all the movies
                movieList = ReadWebPage(url);

                //Mine url for all the songs from the movie list
                foreach (string movieUrl in movieList)
                {
                    string movieName = string.Empty;
                    int lastIndex = 0;

                    lastIndex = movieUrl.LastIndexOf(@"/");
                    movieName = movieUrl.Substring(lastIndex + 1).Replace('-', ' ');
                    movieName = movieName.Substring(0, 1).ToUpper() + movieName.Substring(1).ToLower();

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(movieUrl);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        string line;
                        bool found = false;
                        string songTitle = string.Empty;
                        string movieLink = string.Empty;

                        songTitle = "XXXXXXXX";

                        while ((line = sr.ReadLine()) != null)
                        {

                            movieLink = "";

                            //Check if the web page corresponds to the movie name
                            if (line.Contains(movieName))
                            {
                                found = true;
                            }

                            //If the movie is found
                            if (found)
                            {
                                if (line.Contains("http://www.apunkabollywood.us/browser/download/get") &&
                                    !line.Contains(songTitle))
                                {
                                    int posFirstQuote = line.IndexOf("http://www.apunkabollywood.us");
                                    int posSecondQuote = line.IndexOf('"', posFirstQuote + 1);
                                    movieLink = line.Substring(posFirstQuote, posSecondQuote - posFirstQuote);

                                    int posAtag = line.IndexOf("</a>");
                                    songTitle = line.Substring(posSecondQuote + 2, posAtag - posSecondQuote - 2).Trim();
                                    movieSonglist.Add(new MovieSongs() { MovieName = movieName, SongTitle = songTitle, DownloadLink = movieLink });
                                }
                            }
                        }
                    }
                }

                string PreviousMovieName = string.Empty;

                //Loop through list of songs and download one by one
                foreach (MovieSongs ms in movieSonglist)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ms.DownloadLink);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    if(!PreviousMovieName.Equals(ms.MovieName))
                    {
                        Logger("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
                        Logger(string.Format("Movie: {0}", ms.MovieName));
                        Logger("-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
                    }

                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.Contains(".mp3"))
                            {
                                int posFirstQuote = line.IndexOf("http://download");
                                int posSecondQuote = line.IndexOf('"', posFirstQuote + 1);
                                string mp3DownloadLink = line.Substring(posFirstQuote, posSecondQuote - posFirstQuote);
                                DownloadSong(mp3DownloadLink, ms.SongTitle);
                                Logger(string.Format("Song: {0} downloaded successfully!!", ms.SongTitle));
                                Console.WriteLine("{0} - {1} downloaded successfully...", ms.MovieName, ms.SongTitle);
                            }
                        }
                    }

                    PreviousMovieName = ms.MovieName;
                }

                //bodyNoWhiteSpace = Regex.Replace(body, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                Logger("");
                Logger(" -: End of Processing :-");
                Console.WriteLine("End of processing...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logger(ex.Message);
                Logger("Batch processing terminated with errors.");
                Console.WriteLine(ex.Message);
            }
            finally {
                
            }
        }

        public static void DownloadSong(string url, string songTitle)
        {
            string mp3FileName = songTitle + ".mp3";
            WebClient wc = new WebClient();
            Uri uri = new Uri(url);
            wc.DownloadFile(uri, mp3FileName);
        }

        public static void DownloadSong(string url)
        {
            WebClient wc = new WebClient();
            Uri uri = new Uri(url);
            wc.DownloadFile(uri, "test.mp3");
        }


        public static List<string> ReadWebPage(string url)
        {
            List<string> movieList = new List<string>();
            bool movieListFound = false;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    //Check if the Div "Categories" is available 
                    if (line.Contains("categories"))
                    {
                        movieListFound = true;
                    }

                    //If the Div "Categories" is found then find the first list <li> tag
                    //and parse the url
                    if (movieListFound)
                    {
                        if (line.Contains("<li>"))
                        {
                            int posFirstQuote = line.IndexOf('"');
                            int posSecondQuote = line.IndexOf('"', posFirstQuote + 1);
                            string movieLink = line.Substring(posFirstQuote + 1, posSecondQuote - posFirstQuote - 1);
                            movieList.Add(movieLink);
                        }
                    }

                    //When the user list ends then stop populating the movie list.
                    if (movieListFound && line.Contains("</ul>"))
                    {
                        movieListFound = false;
                        break;
                    }
                }

            }

            return movieList;
        }

        public static void Logger(string log)
        {
            StreamWriter sw = new StreamWriter("DownloadLogfile.txt", true);
            sw.WriteLine(log);
            sw.Flush();
            sw.Close();
        }

    }
}
