# iOS
LaunchScreen.storyboard in Assets/Graphics

Unity:
* Project Settings -> Player -> Other Settings -> Version = bump
* build for iOS via build settings

Xcode
* drag in icon.png to xcode project left sidebar
  * AppIcon already set (see Images.xcassets in left sidebar)
* Signing & Capabilities -> Signing -> Team = Jonathan Zheng
* General -> Identity -> app category = Games - Educational Games
  * Display Name = EcoBuilder
  * Version = bump
  * Build = bump

hit play button in top left to build!

submit to app store:
https://developer.apple.com/documentation/xcode/distributing-your-app-for-beta-testing-and-releases

Product -> Archive
Window -> Organizer

# Android
* Build aab file
* DO NOT LOSE .keystore FILE because you must sign it with the same key to update the same Android app