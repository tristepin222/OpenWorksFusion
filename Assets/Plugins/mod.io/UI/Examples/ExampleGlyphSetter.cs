﻿using System.Collections;
using ModIOBrowser.Implementation;
using UnityEngine;

namespace Plugins.mod.io.UI.Examples
{
    public class ExampleGlyphSetter : MonoBehaviour
    {
        bool connected;

        void Awake()
        {
            GlyphPlatforms glyphs = Application.platform switch
            {
                RuntimePlatform.PS4                => GlyphPlatforms.PLAYSTATION_4,
                RuntimePlatform.XboxOne            => GlyphPlatforms.XBOX,
                RuntimePlatform.Switch             => GlyphPlatforms.NINTENDO_SWITCH,
                RuntimePlatform.GameCoreXboxSeries => GlyphPlatforms.XBOX,
                RuntimePlatform.GameCoreXboxOne    => GlyphPlatforms.XBOX,
                RuntimePlatform.PS5                => GlyphPlatforms.PLAYSTATION_5,
                _                                  => GlyphPlatforms.PC
            };

            Glyphs.Instance.ChangeGlyphs(glyphs);

            if (glyphs == GlyphPlatforms.PC) StartCoroutine(CheckForControllers());
        }

        IEnumerator CheckForControllers()
        {
            while (true)
            {
                var controllers = Input.GetJoystickNames();

                if (!connected && controllers.Length > 0)
                {
                    connected = true;
                    if(controllers[0].Contains("Microsoft"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.XBOX);
                    }
                    else if(controllers[0].Contains("Sony"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.PLAYSTATION_5);
                    }
                    else if(controllers[0].Contains("Nintendo"))
                    {
                        Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.NINTENDO_SWITCH);
                    }

                    Debug.Log("Connected");

                }
                else if (connected && controllers.Length == 0)
                {
                    connected = false;
                    Glyphs.Instance.ChangeGlyphs(GlyphPlatforms.PC);
                    Debug.Log("Disconnected");
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }
}
