using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Janelia
{
    // Regression tests for the four-square progress box in `PanoramicDisplayCamera`
    // (https://github.com/JaneliaSciComp/janelia-unity-toolkit/pull/232). They target the API from the
    // commit "Simplifies the progress box API": `progressBoxPosition` is the center of the 2x2 grid,
    // `progressBoxSeparation` is the gap between the squares, and `progressBoxSaveDelegate` replaces
    // `PlayerPrefs` as the place where interactive changes are saved.
    // These tests are written to fail on the code as submitted in that PR.
    //
    // Note that `SetUp` and `TearDown` delete the progress box keys from `PlayerPrefs`, so running
    // these tests in the editor discards any progress box position saved on this machine.
    public class PanoramicDisplayCameraProgressBoxTest
    {
        private const string PREFS_SHOW = "PanoramicDisplayCamera.ShowProgressBox";
        private const string PREFS_POSITION_X = "PanoramicDisplayCamera.ProgressBoxPositionX";
        private const string PREFS_POSITION_Y = "PanoramicDisplayCamera.ProgressBoxPositionY";
        private const string PREFS_SEPARATION_X = "PanoramicDisplayCamera.ProgressBoxSeparationX";
        private const string PREFS_SEPARATION_Y = "PanoramicDisplayCamera.ProgressBoxSeparationY";

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

        // `Start()` reads `PlayerPrefs` unconditionally and so overwrites the position and separation that
        // other code set before the first frame. The comment on `progressBoxSaveDelegate` says that this is
        // exactly what code using the delegate should do: restore the changes "by setting the
        // `progressBoxPosition` and other properties directly at startup".
        [UnityTest]
        public IEnumerator StartDoesNotOverwritePositionWhenSaveDelegateIsInstalled()
        {
            // Stale values from an earlier run, left behind in `PlayerPrefs`.
            PlayerPrefs.SetInt(PREFS_POSITION_X, 999);
            PlayerPrefs.SetInt(PREFS_POSITION_Y, 888);
            PlayerPrefs.SetInt(PREFS_SEPARATION_X, 77);
            PlayerPrefs.SetInt(PREFS_SEPARATION_Y, 66);

            // A realistic texture size, so that the (500, 400) position below is on the texture and the
            // validity reset in `Start()` leaves it alone.
            MakeComponent(640, 480);
            _pdc.progressBoxSaveDelegate = (show, position, separation) => { };
            _pdc.progressBoxPosition = new Vector2Int(500, 400);
            _pdc.progressBoxSeparation = new Vector2Int(5, 6);

            yield return null;  // `Start()` runs here.

            Assert.AreEqual(new Vector2Int(500, 400), _pdc.progressBoxPosition);
            Assert.AreEqual(new Vector2Int(5, 6), _pdc.progressBoxSeparation);
        }

        // The delegate receives `show` along with the position and the separation, so code using it owns all
        // three values. `Start()` must not restore `showProgressBox` from `PlayerPrefs`, or the two storage
        // mechanisms are mixed.
        [UnityTest]
        public IEnumerator StartDoesNotRestoreVisibilityFromPlayerPrefsWhenSaveDelegateIsInstalled()
        {
            PlayerPrefs.SetInt(PREFS_SHOW, 1);

            MakeComponent();
            _pdc.progressBoxSaveDelegate = (show, position, separation) => { };
            _pdc.showProgressBox = false;

            yield return null;

            Assert.IsFalse(_pdc.showProgressBox);
        }

        // `ProgressBoxTexturePosition()` splits the separation between the two columns (and rows) with
        // `Mathf.CeilToInt(sep.x / 2)` and `Mathf.FloorToInt(sep.x / 2)`. Both arguments are `int`, so the
        // division truncates before the rounding functions see it, and both halves come out equal to
        // `sep.x / 2`. For an odd separation, the squares end up one pixel closer than requested. For the
        // smallest separation that `ProgressBoxIsValid()` accepts, 1, the squares touch.
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        public void SeparationIsTheGapBetweenTheSquares(int separation)
        {
            MakeComponent();
            _pdc.progressBoxSize = 50;
            Vector2Int center = new Vector2Int(500, 400);
            Vector2Int sep = new Vector2Int(separation, separation);

            Vector2Int topLeft = TexturePosition(0, center, sep);
            Vector2Int topRight = TexturePosition(1, center, sep);
            Vector2Int bottomLeft = TexturePosition(2, center, sep);

            Assert.AreEqual(separation, topRight.x - (topLeft.x + _pdc.progressBoxSize), "horizontal gap");
            Assert.AreEqual(separation, bottomLeft.y - (topLeft.y + _pdc.progressBoxSize), "vertical gap");
        }

        // `progressBoxPosition` and `progressBoxSeparation` have no initializers, so a component added in
        // the Inspector starts with both at (0, 0). `ProgressBoxIsValid()` rejects a separation below 1 in
        // either dimension, and a position of (0, 0) puts the left column off the screen, so the box never
        // draws. The keyboard cannot repair this: each key changes one component by one pixel, and
        // `Update()` discards any change that does not already produce a valid box, so from the defaults
        // every key press is rejected. The previous version of this PR had working defaults.
        [Test]
        public void DefaultPositionAndSeparationAreValid()
        {
            MakeComponent(640, 480);

            Assert.IsTrue(IsValid(_pdc.progressBoxPosition, _pdc.progressBoxSeparation),
                $"position {_pdc.progressBoxPosition}, separation {_pdc.progressBoxSeparation}");
        }

        private void MakeComponent(int dataWidth = 2, int dataHeight = 2)
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

            int n = dataWidth * dataHeight;
            _pdc.SetDisplaySurfaceData(dataWidth, dataHeight, new float[n], new float[n], new float[n], new byte[n], new Color[n]);
        }

        // The two helpers below reach private methods through reflection, as the geometry of the box is
        // otherwise observable only in the rendered image.

        private Vector2Int TexturePosition(int i, Vector2Int position, Vector2Int separation)
        {
            MethodInfo method = typeof(PanoramicDisplayCamera).GetMethod("ProgressBoxTexturePosition",
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] { typeof(int), typeof(Vector2Int), typeof(Vector2Int) }, null);
            Assert.IsNotNull(method, "PanoramicDisplayCamera.ProgressBoxTexturePosition(int, Vector2Int, Vector2Int) not found");
            return (Vector2Int)method.Invoke(_pdc, new object[] { i, position, separation });
        }

        private bool IsValid(Vector2Int position, Vector2Int separation)
        {
            MethodInfo method = typeof(PanoramicDisplayCamera).GetMethod("ProgressBoxIsValid",
                BindingFlags.NonPublic | BindingFlags.Instance, null,
                new[] { typeof(Vector2Int), typeof(Vector2Int), typeof(bool) }, null);
            Assert.IsNotNull(method, "PanoramicDisplayCamera.ProgressBoxIsValid(Vector2Int, Vector2Int, bool) not found");
            return (bool)method.Invoke(_pdc, new object[] { position, separation, false });
        }

        private static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(PREFS_SHOW);
            PlayerPrefs.DeleteKey(PREFS_POSITION_X);
            PlayerPrefs.DeleteKey(PREFS_POSITION_Y);
            PlayerPrefs.DeleteKey(PREFS_SEPARATION_X);
            PlayerPrefs.DeleteKey(PREFS_SEPARATION_Y);
        }
    }
}
