using Sdl.Web.Mvc.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace Sdl.Web.Mvc
{
    /// <summary>
    /// Class to manage static assets such as config and resources. This contains generic methods to serialize assets
    /// based on a root boostrap json file, which recursively loads in more assets. 
    /// Implementations of this class are responsible for implementing the Serialize method
    /// in order to read the static asset from somewhere (eg Broker DB) and serialize it to the file system
    /// </summary>
    public abstract class BaseStaticFileManager : IStaticFileManager
    {
        protected const string TEMP_DIR_SUFFIX = "_temp";
        
        public virtual void CreateStaticAssets(string applicationRoot)
        {
            List<string> folders = new List<string>();
            try
            {
                foreach (var loc in Configuration.Localizations.Values)
                {
                    var versionRoot = String.Format("{0}{1}/{2}/{3}", applicationRoot, loc.Path, Configuration.SystemFolder, Configuration.SiteVersion);
                    if (!folders.Contains(versionRoot))
                    {
                        //If the version already exists, we do nothing
                        if (!Directory.Exists(versionRoot))
                        {
                            //Create a temp dir - if everything succeeds we rename this
                            var tempVersionRoot = versionRoot + TEMP_DIR_SUFFIX;
                            var di = Directory.CreateDirectory(tempVersionRoot);
                            Log.Debug("Created temp version root: {0}", tempVersionRoot);
                            //Find bootstrap file and take it from there.
                            var url = String.Format("{0}/{1}/_all.json", loc.Path == "" || loc.Path.StartsWith("/") ? loc.Path : "/" + loc.Path, Configuration.SystemFolder);
                            SerializeFile(url, applicationRoot, String.Format("/{0}{1}", Configuration.SiteVersion, TEMP_DIR_SUFFIX), 2);
                            folders.Add(versionRoot);
                        }
                        else
                        {
                            Log.Debug("Version root {0} already exists. Nothing to do", versionRoot);
                        }
                    }
                }
                //If all temp folders were created OK, rename them to be the real version folder
                foreach (var folder in folders)
                {
                    Directory.Move(folder + TEMP_DIR_SUFFIX, folder);
                    Log.Debug("Renamed temp version root to : {0}", folder);
                }
                //finally update the current version - we only do this if everything worked!
                Configuration.CurrentVersion = Configuration.SiteVersion;
                Log.Debug("Current version is now {0}", Configuration.CurrentVersion);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating files on disk.");
                //If something goes wrong we need to delete all temp and newly created version folders, to ensure we don't have a partial version
                foreach (var folder in folders)
                {
                    if (Directory.Exists(folder + TEMP_DIR_SUFFIX))
                    {
                        Directory.Delete(folder + TEMP_DIR_SUFFIX);
                    }
                    else if (Directory.Exists(folder))
                    {
                        Directory.Delete(folder);
                    }
                }
            }
            finally
            {
                //TODO Delete old versions
            }
        }

        public abstract string Serialize(string url, string applicationRoot, string suffix, bool returnContents = false);

        /// <summary>
        /// Recursively serialize a file (which may contain just a list of further files to recursively process)
        /// </summary>
        /// <param name="url">The url of the file</param>
        /// <param name="applicationRoot">The root file path of the application</param>
        /// <param name="suffix">An optional suffix that can be injected into the file url - to enable version numbers and temp directories to be used in the real file path</param>
        /// <param name="bootstrapLevel">Level of recursion expected 0=none (the file contains data to serialize rather than a list of other files)</param>
        protected virtual void SerializeFile(string url, string applicationRoot, string suffix, int bootstrapLevel = 0)
        {
            string fileContents = this.Serialize(url, applicationRoot, suffix, bootstrapLevel != 0);
            if (bootstrapLevel != 0)
            {
                var bootstrapJson = Json.Decode(fileContents);
                foreach (string file in bootstrapJson.files)
                {
                    SerializeFile(file, applicationRoot, suffix, bootstrapLevel - 1);
                }
            }
        }
    }
}
