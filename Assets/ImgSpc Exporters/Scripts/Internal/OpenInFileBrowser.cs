/// <summary>
/// ImgSpc Exporters.
/// Copyright 2015 Imaginary Spaces.
/// http://imgspc.com
/// 
/// Opens the given path in a file browser.
/// Works for Windows/Mac/Linux.
/// Heavily modified from the public-domain code in:
/// http://wiki.unity3d.com/index.php/OpenInFileBrowser
/// </summary>

#if UNITY_STANDALONE || UNITY_EDITOR
namespace ImgSpc.Exporters 
{
    public static class OpenInFileBrowser
    {
        #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        const char goodSlash = '\\';
        const char badSlash = '/';
        const char quoteChar = '"';
        const string commandName = "explorer";
        #else
        const char goodSlash = '/';
        const char badSlash = '\\';
        const char quoteChar = '\'';
        #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        const string commandName = "open";
        #elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        // TODO: try a few different methods, launch an error dialog if none of them work.
        const string commandName = "xdg-open";
        #else
        // TODO: ios/android/webplayer/consoles
        const string commandName = null;
        #endif
        #endif

        /// <summary>
        /// This function does 3 things:
        /// 1. Convert to backslash or foreslash depending on platform.
        /// 2. Properly quote the path so that we can pass the result as an argument in a shell execution.
        /// 3. Quote the whole string so the shell will treat it as one argument.
        /// 
        /// TODO: do we need to do more quoting on windows?  Its shell expansion is fucking insane, so maybe we do.
        /// http://www.robvanderwoude.com/escapechars.php
        /// On unix we don't, because we're using single-quotes.  Nothing interpolates inside single-quotes.
        /// </summary>
        static string QuotePath (string path)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append('"');
            foreach (var c in path) {
                switch (c) {
                case badSlash:
                    // replace the bad backslash with the good backslash
                    builder.Append(goodSlash);
                    break;

                default:
                    builder.Append(c);
                    break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        /// <summary>
        /// Open the path in a file browser.
        /// If the path points to a file, open the file browser up to the directory, and highlight the file
        /// (if supported on your platform).
        /// 
        /// If "log" is true we emit some stuff to the player log before trying.
        /// We always emit to the player log if there's an exception, but generally
        /// we don't get to hear whether there was an error opening the directory.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="log">If set to <c>true</c> log.</param>
        public static void Open (string path, bool log = false)
        {
            // Convert Unix-to-DOS or v-v so that users can paste in a path from
            // someone on the other platform.
            path = path.Replace(badSlash, goodSlash);

            // Begin building the arguments.
            var builder = new System.Text.StringBuilder();

            // Add in the arguments.  Remember to quote the path.
            if (System.IO.Directory.Exists(path)) {
                // Directory; open it.  On all platforms the argument is just the directory name.
                builder.Append(QuotePath(path));
            } else {
                // Open the containing folder and highlight the file.  How that works depends on platform.

                #if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                builder.Append("/select,"); // important: no space!
                builder.Append(QuotePath(path));

                #elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                builder.Append("-R "); // important: space!
                builder.Append(QuotePath(path));

                #elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                // xdg-open does not have an option to open the directory and highlight the file,
                // so just open the directory.
                builder.Append(QuotePath(System.IO.Path.GetDirectoryName(path)));
                #endif
            }

            try {
                if (commandName!=null) {    
                    if (log) {
                        UnityEngine.Debug.Log(string.Format("running {0} with arguments [{1}]", commandName, builder.ToString()));
                    }
                    System.Diagnostics.Process.Start(commandName, builder.ToString());
                }
            } catch (System.ComponentModel.Win32Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
#endif
