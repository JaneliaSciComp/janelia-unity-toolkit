using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Janelia
{
    // Regression tests for the four-square progress box in `PanoramicDisplayCamera`
    // (https://github.com/JaneliaSciComp/janelia-unity-toolkit/pull/232).
    // These are written to fail on the code as submitted in that PR.
    //
    // Note that `SetUp` and `TearDown` delete the progress box keys from `PlayerPrefs`, so running
    // these tests in the editor discards any progress box position saved on this machine.
    public class PanoramicDisplayCameraProgressBoxTest
    {
        private const string PREFS_PREFIX = "PanoramicDisplayCamera.ProgressBox";
        private const string PREFS_SHOW = "PanoramicDisplayCamera.ShowProgressBox";

        private GameObject _root;
        private PanoramicDisplayCamera _pdc;

        [SetUp]
        public void SetUp()
        {
            ClearPrefs();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }
            ClearPrefs();
        }

        // `Update()` computes `sum / progressBoxPosition.Length` unconditionally, so an array that the
        // Inspector resized to 0 throws `DivideByZeroException` on every frame, even though
        // `showProgressBox` is false. The exception aborts the rest of `Update()`.
        [UnityTest]
        public IEnumerator UpdateSurvivesEmptyPositionArray()
        {
            MakeComponent();
            _pdc.showProgressBox = false;
            _pdc.progressBoxPosition = new Vector2Int[0];

            yield return null;
            yield return null;

            // Unity reports an exception thrown from `Update()` as an error log message.
            LogAssert.NoUnexpectedReceived();
        }

        // Same as above, for an array set to `null` from code.
        [UnityTest]
        public IEnumerator UpdateSurvivesNullPositionArray()
        {
            MakeComponent();
            _pdc.showProgressBox = false;
            _pdc.progressBoxPosition = null;

            yield return null;
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        // `Start()` reads `PlayerPrefs` unconditionally and so overwrites positions that other code set
        // before the first frame. The comment on `playerPreferencesAlternative` says that this is exactly
        // what code using the delegate should do: restore the positions "by setting the `progressBoxPosition`
        // property directly at startup".
        [UnityTest]
        public IEnumerator StartDoesNotOverwritePositionsWhenAlternativeIsInstalled()
        {
            // Stale values from an earlier run, left behind in `PlayerPrefs`.
            PlayerPrefs.SetInt(PREFS_PREFIX + "0PositionX", 999);
            PlayerPrefs.SetInt(PREFS_PREFIX + "0PositionY", 888);

            MakeComponent();
            _pdc.playerPreferencesAlternative = (positions) => { };
            _pdc.progressBoxPosition = new Vector2Int[] {
                new Vector2Int(5, 6), new Vector2Int(7, 8), new Vector2Int(9, 10), new Vector2Int(11, 12)
            };

            yield return null;  // `Start()` runs here.

            Assert.AreEqual(new Vector2Int(5, 6), _pdc.progressBoxPosition[0]);
        }

        // The delegate receives only the positions, so code using it cannot persist `showProgressBox`.
        // Yet `Start()` still restores `showProgressBox` from `PlayerPrefs`, so the two storage
        // mechanisms are mixed.
        [UnityTest]
        public IEnumerator StartDoesNotRestoreVisibilityFromPlayerPrefsWhenAlternativeIsInstalled()
        {
            PlayerPrefs.SetInt(PREFS_SHOW, 1);

            MakeComponent();
            _pdc.playerPreferencesAlternative = (positions) => { };
            _pdc.showProgressBox = false;

            yield return null;

            Assert.IsFalse(_pdc.showProgressBox);
        }

        private void MakeComponent()
        {
            // `Update()` dereferences `_material` before it reaches the progress box code, and `_material`
            // needs this shader. Without it, every test here would fail for an unrelated reason.
            if (Resources.Load("PanoramicDisplay", typeof(Shader)) == null)
            {
                Assert.Inconclusive("PanoramicDisplay.shader not found in a Resources folder");
            }

            _root = new GameObject("PanoramicDisplayCameraProgressBoxTest");
            _root.AddComponent<Camera>();

            Camera[] sources = new Camera[6];
            for (int i = 0; i < 6; ++i)
            {
                GameObject child = new GameObject("source" + i);
                child.transform.SetParent(_root.transform);
                sources[i] = child.AddComponent<Camera>();
            }

            _pdc = _root.AddComponent<PanoramicDisplayCamera>();
            _pdc.sourceCameras = sources;
            _pdc.sourceWidth = 16;

            int w = 2, h = 2, n = w * h;
            _pdc.SetDisplaySurfaceData(w, h, new float[n], new float[n], new float[n], new byte[n], new Color[n]);
        }

        private static void ClearPrefs()
        {
            for (int i = 0; i < 4; ++i)
            {
                PlayerPrefs.DeleteKey(PREFS_PREFIX + i + "PositionX");
                PlayerPrefs.DeleteKey(PREFS_PREFIX + i + "PositionY");
            }
            PlayerPrefs.DeleteKey(PREFS_SHOW);
        }
    }
}
