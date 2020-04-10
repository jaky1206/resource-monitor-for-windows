using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UrlHistoryLibrary;

namespace WindowsResourceMonitor
{   
    public class URL
    {
        string url;
        string title;
        string browser;
        public URL(string url, string title, string browser)
        {
            this.url = url;
            this.title = title;
            this.browser = browser;
        }
    }
    class InternetExplorer
    {
        // List of URL objects
        public List<URL> URLs { get; set; }
        public IEnumerable<URL> GetHistory()
        {
            try
            {
                // Initiate main object
                UrlHistoryWrapperClass urlhistory = new UrlHistoryWrapperClass();


                // Enumerate URLs in History
                UrlHistoryWrapperClass.STATURLEnumerator enumerator =

                                                   urlhistory.GetEnumerator();

                // Iterate through the enumeration
                while (enumerator.MoveNext())
                {
                    // Obtain URL and Title
                    string url = enumerator.Current.URL.Replace('\'', ' ');
                    // In the title, eliminate single quotes to avoid confusion
                    string title = string.IsNullOrEmpty(enumerator.Current.Title)
                              ? enumerator.Current.Title.Replace('\'', ' ') : "";

                    // Create new entry
                    URL U = new URL(url, title, "Internet Explorer");

                    // Add entry to list
                    URLs.Add(U);
                }

                // Optional
                enumerator.Reset();

                // Clear URL History
                urlhistory.ClearHistory();

                return URLs;
            }
            catch(Exception ex)
            {
                return URLs;
            }
        }
    }
}
