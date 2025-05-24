<div align="center">
     <img src="YtDlpExtension/Assets/StoreLogo.scale-100.png" alt="Logo"/>
     <div><h1>  Video Downloader<br><p>For PowerToys</p></h1><br></div>
</div>
<div align="center">     
<img src="Images/SneakPeek.png" style="width: 900px" alt="SneakPeek"/>
</div>


# 📝 Note
This extension is currently in development, and a release will be out soon.



# 🚀 Effortless Video Downloads with PowerToys!
Download videos, audios, and playlists directly from the Command Palette with ease.\
Easily download videos, audios, playlists, and (soon™) captions directly within PowerToys Command Palette using yt-dlp.
# ✨ Key Features
- Download videos from hundreds of websites ([supported websites](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md)).
- Simplified management: Choose output directories, set formats, and monitor progress directly from the interface.
- Parallel download: Download each video format individually with progress tracking.
- Quick audio download: Download audio with a single command.
- Playlist download support!
- Video trimming: Trim videos by setting start and end times before download
- Captions and subtitles: Download auto-captions or subtitles from a video 
- Customized output format and merging support
- Quick merging using yt-dlp's format selector expressions

# ⚙️ Settings
- **Download location:** the destination directory to download (default: `User Downloads folder`)
- **Video Output format:** the output format (Container) of all downloaded videos (default: `mp4`)
- **Audio Output format:** the output format of all downloaded audios (default: `mp3`)
- **Custom format selector:** sets a custom yt-dlp [format selector expression string](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#format-selection). If a custom string is set, the audio and video output setting will be ignored (default: `blank`)\
(**It's recommended to leave this field blank if you don't need a specific video or audio codec**)\
Editing software like `Premiere`, `After Effects`, `DaVinci Resolve`, may not recognize videos encoded with `VP9` or `AV1`, prefer videos encoded with `AVC1` for broader compatibility.

# 🚨 Known Issues
 - **Download Speeds:**\
 Download speeds may be slower than expected for some sites and formats, especially for larger files. This extension does not implement any additional download acceleration mechanisms beyond those provided by yt-dlp. 
 Some users have reported that passing browser cookies to yt-dlp improves download speeds (details below).
 - **Age restricted videos:**\
 Currently, downloading age-restricted or login-protected videos is not supported by this extension. yt-dlp typically requires additional configuration, such as account authentication or cookies, to access such content. There will be a settings option in the future to select cookies from browser to pass to yt-dlp, ...however, this involves certain risks you should be aware of.

# 🤝 Contributing

This is my first open source project, so I'm especially open to feedback, ideas, and contributions.  
Feel free to share suggestions or improvements!

# 💼 TODO
- [ ] Fully implement QuickMerge feature
- [ ] Implement the trimming form page
- [ ] Implement captions and subtitle download
- [ ] Add a context command to download the video thumbnail
