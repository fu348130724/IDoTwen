//
// hbx_WebglRetinaTools.cs v2.0
// Written by Thomas Hogarth, Hogbox Studios Ltd
// Developed against WebGL build from Unity 5.6.0f3
//

//#define BROTLISTREAM_AVALIABLE

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

#if BROTLISTREAM_AVALIABLE
using Brotli;
#endif

namespace hbx {

	public static class WebGLRetinaToolsLite
	{
		const string VERSION_STR = "2.0"; 

		static WebGLRetinaToolsLite()
		{
#if BROTLISTREAM_AVALIABLE
		    String currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			String dllPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "Assets"), "Plugins");
		    if(!currentPath.Contains(dllPath))
		    {
		        Environment.SetEnvironmentVariable("PATH", currentPath + Path.PathSeparator + dllPath, EnvironmentVariableTarget.Process);
		    }
#endif
		}
		const string ProgressTitle = "Applying Retina Fix";
		const string JsExt = ".js";
		const string JsgzExt = ".jsgz";
		const string JsbrExt = ".jsbr";
		const string UnitywebExt = ".unityweb";
		#if UNITY_5_6_OR_NEWER
		const string DevFolder = "Build";
		static readonly string[] SourceFileTypes = {JsExt, UnitywebExt};
		static readonly string[] ExcludeFileNames = {"asm.memory", ".asm.code", ".data", "wasm.code"};
		#else
		const string DevFolder = "Development";
		static readonly string[] SourceFileTypes = {JsExt, JsgzExt, JsbrExt};
		static readonly string[] ExcludeFileNames = {"UnityLoader"};
		#endif
		
		enum CompressionType {
			None,
			GZip,
			Brotli
		};

		[MenuItem("Hbx/WebGL Tools Lite/Retina Fix Last Build", false, 0)]
		public static void RetinaFixLastBuild()
		{
			if(EditorUserBuildSettings.development) {
				RetinaFixCodeFolder(DevFolder);
			} else {
				// only supports dev builds
			}
		}

		//
		// Opens the jsgz and/or the js file in the current webgl build folder 
		// and inserts devicePixelRatio accordingly to add support for retina/hdpi 
		//
		//[MenuItem("Hbx/WebGL Tools/Retina Fix Release Build", false, 11)]
		public static void RetinaFixCodeFolder(string codeFolder)
		{
			UnityEngine.Debug.Log("WebGLRetinaToolsLite: Fix build started.");
			
			// get path of the last webgl build or use over ride path
			string webglBuildPath = EditorUserBuildSettings.GetBuildLocation(BuildTarget.WebGL);
			string codeFolderPath = Path.Combine(webglBuildPath, codeFolder);

			if(string.IsNullOrEmpty(codeFolderPath)) {
				UnityEngine.Debug.LogError("WebGLRetinaToolsLite: WebGL build path is empty, have you created a WebGL build yet?");
				return;
			}
	
			// check there is a release folder
			if(!Directory.Exists(codeFolderPath)) {
				UnityEngine.Debug.LogError("WebGLRetinaToolsLite: Couldn't find folder for WebGL build at path:\n" + codeFolderPath);
				return;
			}

			// find source files in release folder and fix
			string[] sourceFiles = FindSourceFilesInBuildFolder(codeFolderPath);
			foreach(string sourceFile in sourceFiles)
				FixSourceFile(sourceFile);
	
			UnityEngine.Debug.Log("WebGLRetinaToolsLite: Complete fixed " + sourceFiles.Length + " source files.");

			EditorUtility.ClearProgressBar();
		}

		//
		// Fix a source file based on it's extension type
		//
		static void FixSourceFile(string aSourceFile) {
			UnityEngine.Debug.Log("WebGLRetinaToolsLite: Fixing " + aSourceFile);
			FixJSFile(aSourceFile);
		}
		
		//
		// Fix a standard .js file
		//
		static void FixJSFile(string jsPath) {
			string fileName = Path.GetFileName(jsPath);

			EditorUtility.DisplayProgressBar(ProgressTitle, "Opening " + fileName + "...", 0.0f);

			UnityEngine.Debug.Log("WebGLRetinaToolsLite: Fixing raw JS file " + jsPath);

			// load the uncompressed js code
			string sourcestr = File.ReadAllText(jsPath);
			StringBuilder source = new StringBuilder(sourcestr);
			sourcestr = "";	

			EditorUtility.DisplayProgressBar(ProgressTitle, "Fixing js source in " + fileName + "...", 0.5f);
	
			FixJSFileContents(fileName.Contains(".wasm."), ref source);
	
			EditorUtility.DisplayProgressBar(ProgressTitle, "Saving js " + fileName + "...", 1.0f);
	
			// save the file
			File.WriteAllText(jsPath, source.ToString());
		}
		
		//
		// Search folder path for all supported SourceFileTypes
		// excluding any with names containing ExcludeFileNames
		//
		static string[] FindSourceFilesInBuildFolder(string aBuildPath) {
			string[] files = Directory.GetFiles(aBuildPath);
			List<string> found = new List<string>();
			foreach(string file in files) {
				string ext = Path.GetExtension(file); 
				if(Array.IndexOf(SourceFileTypes, ext) == -1) continue;
				string name = Path.GetFileNameWithoutExtension(file);
				bool exclude = false;
				foreach(string exname in ExcludeFileNames) {
					if(name.Contains(exname)) { exclude = true; break; }
				}
				if(!exclude) found.Add(file);
			}
			return found.ToArray();
		}

		//
		// Perform the find and replace hack for a development source
		//
		static void FixJSFileContents(bool iswasm, ref StringBuilder source) {
			// fix fill mouse event
			if(!iswasm) 
			{

			string findFillMouseString =
@" fillMouseEventData: (function(eventStruct, e, target) {
  HEAPF64[eventStruct >> 3] = JSEvents.tick();
  HEAP32[eventStruct + 8 >> 2] = e.screenX;
  HEAP32[eventStruct + 12 >> 2] = e.screenY;
  HEAP32[eventStruct + 16 >> 2] = e.clientX;
  HEAP32[eventStruct + 20 >> 2] = e.clientY;
  HEAP32[eventStruct + 24 >> 2] = e.ctrlKey;
  HEAP32[eventStruct + 28 >> 2] = e.shiftKey;
  HEAP32[eventStruct + 32 >> 2] = e.altKey;
  HEAP32[eventStruct + 36 >> 2] = e.metaKey;
  HEAP16[eventStruct + 40 >> 1] = e.button;
  HEAP16[eventStruct + 42 >> 1] = e.buttons;
  HEAP32[eventStruct + 44 >> 2] = e[""movementX""] || e[""mozMovementX""] || e[""webkitMovementX""] || e.screenX - JSEvents.previousScreenX;
  HEAP32[eventStruct + 48 >> 2] = e[""movementY""] || e[""mozMovementY""] || e[""webkitMovementY""] || e.screenY - JSEvents.previousScreenY;
  if (Module[""canvas""]) {
   var rect = Module[""canvas""].getBoundingClientRect();
   HEAP32[eventStruct + 60 >> 2] = e.clientX - rect.left;
   HEAP32[eventStruct + 64 >> 2] = e.clientY - rect.top;
  } else {
   HEAP32[eventStruct + 60 >> 2] = 0;
   HEAP32[eventStruct + 64 >> 2] = 0;
  }
  if (target) {
   var rect = JSEvents.getBoundingClientRectOrZeros(target);
   HEAP32[eventStruct + 52 >> 2] = e.clientX - rect.left;
   HEAP32[eventStruct + 56 >> 2] = e.clientY - rect.top;
  } else {
   HEAP32[eventStruct + 52 >> 2] = 0;
   HEAP32[eventStruct + 56 >> 2] = 0;
  }
  JSEvents.previousScreenX = e.screenX;
  JSEvents.previousScreenY = e.screenY;
 }),";

			string replaceFillMouseString = 
@" fillMouseEventData: (function(eventStruct, e, target) {
  var devicePixelRatio = window.devicePixelRatio || 1;
  HEAPF64[eventStruct >> 3] = JSEvents.tick();
  HEAP32[eventStruct + 8 >> 2] = e.screenX*devicePixelRatio;
  HEAP32[eventStruct + 12 >> 2] = e.screenY*devicePixelRatio;
  HEAP32[eventStruct + 16 >> 2] = e.clientX*devicePixelRatio;
  HEAP32[eventStruct + 20 >> 2] = e.clientY*devicePixelRatio;
  HEAP32[eventStruct + 24 >> 2] = e.ctrlKey;
  HEAP32[eventStruct + 28 >> 2] = e.shiftKey;
  HEAP32[eventStruct + 32 >> 2] = e.altKey;
  HEAP32[eventStruct + 36 >> 2] = e.metaKey;
  HEAP16[eventStruct + 40 >> 1] = e.button;
  HEAP16[eventStruct + 42 >> 1] = e.buttons;
  HEAP32[eventStruct + 44 >> 2] = e[""movementX""] || e[""mozMovementX""] || e[""webkitMovementX""] || (e.screenX*devicePixelRatio) - JSEvents.previousScreenX;
  HEAP32[eventStruct + 48 >> 2] = e[""movementY""] || e[""mozMovementY""] || e[""webkitMovementY""] || (e.screenY*devicePixelRatio) - JSEvents.previousScreenY;
  if (Module[""canvas""]) {
   var rect = Module[""canvas""].getBoundingClientRect();
   HEAP32[eventStruct + 60 >> 2] = (e.clientX - rect.left) * devicePixelRatio;
   HEAP32[eventStruct + 64 >> 2] = (e.clientY - rect.top) * devicePixelRatio;
  } else {
   HEAP32[eventStruct + 60 >> 2] = 0;
   HEAP32[eventStruct + 64 >> 2] = 0;
  }
  if (target) {
   var rect = JSEvents.getBoundingClientRectOrZeros(target);
   HEAP32[eventStruct + 52 >> 2] = (e.clientX - rect.left) * devicePixelRatio;
   HEAP32[eventStruct + 56 >> 2] = (e.clientY - rect.top) * devicePixelRatio;
  } else {
   HEAP32[eventStruct + 52 >> 2] = 0;
   HEAP32[eventStruct + 56 >> 2] = 0;
  }
  JSEvents.previousScreenX = e.screenX*devicePixelRatio;
  JSEvents.previousScreenY = e.screenY*devicePixelRatio;
 }),";

			source.Replace(findFillMouseString, replaceFillMouseString);

			} else {

			string findFillMouseString =
@"fillMouseEventData:function (eventStruct, e, target) {
        HEAPF64[((eventStruct)>>3)]=JSEvents.tick();
        HEAP32[(((eventStruct)+(8))>>2)]=e.screenX;
        HEAP32[(((eventStruct)+(12))>>2)]=e.screenY;
        HEAP32[(((eventStruct)+(16))>>2)]=e.clientX;
        HEAP32[(((eventStruct)+(20))>>2)]=e.clientY;
        HEAP32[(((eventStruct)+(24))>>2)]=e.ctrlKey;
        HEAP32[(((eventStruct)+(28))>>2)]=e.shiftKey;
        HEAP32[(((eventStruct)+(32))>>2)]=e.altKey;
        HEAP32[(((eventStruct)+(36))>>2)]=e.metaKey;
        HEAP16[(((eventStruct)+(40))>>1)]=e.button;
        HEAP16[(((eventStruct)+(42))>>1)]=e.buttons;
        HEAP32[(((eventStruct)+(44))>>2)]=e[""movementX""] || e[""mozMovementX""] || e[""webkitMovementX""] || (e.screenX-JSEvents.previousScreenX);
        HEAP32[(((eventStruct)+(48))>>2)]=e[""movementY""] || e[""mozMovementY""] || e[""webkitMovementY""] || (e.screenY-JSEvents.previousScreenY);
  
        if (Module['canvas']) {
          var rect = Module['canvas'].getBoundingClientRect();
          HEAP32[(((eventStruct)+(60))>>2)]=e.clientX - rect.left;
          HEAP32[(((eventStruct)+(64))>>2)]=e.clientY - rect.top;
        } else { // Canvas is not initialized, return 0.
          HEAP32[(((eventStruct)+(60))>>2)]=0;
          HEAP32[(((eventStruct)+(64))>>2)]=0;
        }
        if (target) {
          var rect = JSEvents.getBoundingClientRectOrZeros(target);
          HEAP32[(((eventStruct)+(52))>>2)]=e.clientX - rect.left;
          HEAP32[(((eventStruct)+(56))>>2)]=e.clientY - rect.top;        
        } else { // No specific target passed, return 0.
          HEAP32[(((eventStruct)+(52))>>2)]=0;
          HEAP32[(((eventStruct)+(56))>>2)]=0;
        }
        JSEvents.previousScreenX = e.screenX;
        JSEvents.previousScreenY = e.screenY;
      },";

			string replaceFillMouseString =
@"fillMouseEventData:function (eventStruct, e, target) {
		var devicePixelRatio = window.devicePixelRatio || 1;
        HEAPF64[((eventStruct)>>3)]=JSEvents.tick();
        HEAP32[(((eventStruct)+(8))>>2)]=e.screenX*devicePixelRatio;
        HEAP32[(((eventStruct)+(12))>>2)]=e.screenY*devicePixelRatio;
        HEAP32[(((eventStruct)+(16))>>2)]=e.clientX*devicePixelRatio;
        HEAP32[(((eventStruct)+(20))>>2)]=e.clientY*devicePixelRatio;
        HEAP32[(((eventStruct)+(24))>>2)]=e.ctrlKey;
        HEAP32[(((eventStruct)+(28))>>2)]=e.shiftKey;
        HEAP32[(((eventStruct)+(32))>>2)]=e.altKey;
        HEAP32[(((eventStruct)+(36))>>2)]=e.metaKey;
        HEAP16[(((eventStruct)+(40))>>1)]=e.button;
        HEAP16[(((eventStruct)+(42))>>1)]=e.buttons;
        HEAP32[(((eventStruct)+(44))>>2)]=e[""movementX""] || e[""mozMovementX""] || e[""webkitMovementX""] || ((e.screenX*devicePixelRatio)-JSEvents.previousScreenX);
        HEAP32[(((eventStruct)+(48))>>2)]=e[""movementY""] || e[""mozMovementY""] || e[""webkitMovementY""] || ((e.screenY*devicePixelRatio)-JSEvents.previousScreenY);
  
        if (Module['canvas']) {
          var rect = Module['canvas'].getBoundingClientRect();
          HEAP32[(((eventStruct)+(60))>>2)]=(e.clientX - rect.left)*devicePixelRatio;
          HEAP32[(((eventStruct)+(64))>>2)]=(e.clientY - rect.top)*devicePixelRatio;
        } else { // Canvas is not initialized, return 0.
          HEAP32[(((eventStruct)+(60))>>2)]=0;
          HEAP32[(((eventStruct)+(64))>>2)]=0;
        }
        if (target) {
          var rect = JSEvents.getBoundingClientRectOrZeros(target);
          HEAP32[(((eventStruct)+(52))>>2)]=(e.clientX - rect.left)*devicePixelRatio;
          HEAP32[(((eventStruct)+(56))>>2)]=(e.clientY - rect.top)*devicePixelRatio;        
        } else { // No specific target passed, return 0.
          HEAP32[(((eventStruct)+(52))>>2)]=0;
          HEAP32[(((eventStruct)+(56))>>2)]=0;
        }
        JSEvents.previousScreenX = e.screenX*devicePixelRatio;
        JSEvents.previousScreenY = e.screenY*devicePixelRatio;
      },";

			source.Replace(findFillMouseString, replaceFillMouseString);

			}

	
#if UNITY_5_6_OR_NEWER
			// fix SystemInfo screen width height 
			string findSystemInfoString = 
@"    return {
      width: screen.width ? screen.width : 0,
      height: screen.height ? screen.height : 0,
      browser: browser,";

			string replaceSystemInfoString = 
@"    return {
      devicePixelRatio: window.devicePixelRatio ? window.devicePixelRatio : 1,
      width: screen.width ? screen.width * this.devicePixelRatio : 0,
      height: screen.height ? screen.height * this.devicePixelRatio : 0,
      browser: browser,";
#else
			// fix SystemInfo screen width height 
			string findSystemInfoString = 
@"var systemInfo = {
 get: (function() {
  if (systemInfo.hasOwnProperty(""hasWebGL"")) return this;
  var unknown = ""-"";
  this.width = screen.width ? screen.width : 0;
  this.height = screen.height ? screen.height : 0;";

			string replaceSystemInfoString = 
@"var systemInfo = {
 get: (function() {
  if (systemInfo.hasOwnProperty(""hasWebGL"")) return this;
  var unknown = ""-"";
  var devicePixelRatio = window.devicePixelRatio || 1;
  this.width = screen.width ? screen.width*devicePixelRatio : 0;
  this.height = screen.height ? screen.height*devicePixelRatio : 0;";
#endif
			

			source.Replace(findSystemInfoString, replaceSystemInfoString);
	

			// fix _JS_SystemInfo_GetCurrentCanvasHeight
			
			string findGetCurrentCanvasHeightString = !iswasm ?
@"function _JS_SystemInfo_GetCurrentCanvasHeight() {
 return Module[""canvas""].clientHeight;
}" : 
@"function _JS_SystemInfo_GetCurrentCanvasHeight() 
  	{
  		return Module['canvas'].clientHeight;
  	}";

			string replaceGetCurrentCanvasHeightString =
@"function _JS_SystemInfo_GetCurrentCanvasHeight() {
 var devicePixelRatio = window.devicePixelRatio || 1;
 return Module[""canvas""].clientHeight*devicePixelRatio;
}";

			source.Replace(findGetCurrentCanvasHeightString, replaceGetCurrentCanvasHeightString);
	

			// fix get _JS_SystemInfo_GetCurrentCanvasWidth

			string findGetCurrentCanvasWidthString = !iswasm ?
@"function _JS_SystemInfo_GetCurrentCanvasWidth() {
 return Module[""canvas""].clientWidth;
}" :
@"function _JS_SystemInfo_GetCurrentCanvasWidth() 
  	{
  		return Module['canvas'].clientWidth;
  	}";

			string replaceGetCurrentCanvasWidthString =
@"function _JS_SystemInfo_GetCurrentCanvasWidth() {
 var devicePixelRatio = window.devicePixelRatio || 1;
 return Module[""canvas""].clientWidth*devicePixelRatio;
}";

			source.Replace(findGetCurrentCanvasWidthString, replaceGetCurrentCanvasWidthString);

			
			// fix updateCanvasDimensions

			string findUpdateCanvasString = !iswasm ?
@"else {
   if (canvas.width != wNative) canvas.width = wNative;
   if (canvas.height != hNative) canvas.height = hNative;
   if (typeof canvas.style != ""undefined"") {
    if (w != wNative || h != hNative) {
     canvas.style.setProperty(""width"", w + ""px"", ""important"");
     canvas.style.setProperty(""height"", h + ""px"", ""important"");
    } else {
     canvas.style.removeProperty(""width"");
     canvas.style.removeProperty(""height"");
    }
   }
  }" :
@"else {
          if (canvas.width  != wNative) canvas.width  = wNative;
          if (canvas.height != hNative) canvas.height = hNative;
          if (typeof canvas.style != 'undefined') {
            if (w != wNative || h != hNative) {
              canvas.style.setProperty( ""width"", w + ""px"", ""important"");
              canvas.style.setProperty(""height"", h + ""px"", ""important"");
            } else {
              canvas.style.removeProperty( ""width"");
              canvas.style.removeProperty(""height"");
            }
          }
        }";

			string replaceUpdateCanvasString =
@"else {
   if (canvas.width != wNative) canvas.width = wNative;
   if (canvas.height != hNative) canvas.height = hNative;
   if (typeof canvas.style != ""undefined"") {
    if (w != wNative || h != hNative) {
     canvas.style.setProperty(""width"", w + ""px"", ""important"");
     canvas.style.setProperty(""height"", h + ""px"", ""important"");
    } else {
     //canvas.style.removeProperty(""width"");
     //canvas.style.removeProperty(""height"");
    }
   }
  }";

			source.Replace(findUpdateCanvasString, replaceUpdateCanvasString);
		}
	}

}
