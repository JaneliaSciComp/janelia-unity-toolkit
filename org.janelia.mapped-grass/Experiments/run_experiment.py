import argparse
import os
from pathlib import Path
import subprocess
import sys

def get_unity_log_dir(exe_path):
    exe = Path(exe_path).resolve()
    data_dir = exe.parent / (exe.stem + "_Data")
    app_info = data_dir / "app.info"

    if not app_info.exists():
        raise FileNotFoundError(f"Expected {app_info}")

    lines = app_info.read_text().splitlines()
    company = lines[0].strip()
    product = lines[1].strip()

    log_dir = Path(os.environ["USERPROFILE"]) / "AppData" / "LocalLow" / company / product
    return log_dir

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("-exe", "--exe", help="executable to run")
    parser.add_argument("-duration", "--duration", default=10, help="phase 1 duration (seconds)")
    parser.add_argument("-sample", "--sample", default=1, help="phase 3 sampling period (seconds)")
    parser.add_argument("-hold", "--hold", default=1, help="phase 3 hold per sample (seconds)")
    parser.add_argument("-repeat", "--repeat", default=3, help="phase 3 repeat")
    args = parser.parse_args()

    print(f"Executable: {args.exe}")
    print(f"Duration: {args.duration} sec")

    cmd = f"{args.exe} -timeoutSecs {args.duration} -logFilenameExtra phase1"
    print(f"Phase 1: running `{cmd}`")
    os.system(cmd)

    log_dir = get_unity_log_dir(args.exe)
    recent = sorted(
        (f for f in log_dir.iterdir() if f.is_file() and f.suffix == ".json"),
        key=lambda f: f.stat().st_mtime,
        reverse=True
    )
    log_file = recent[0]
    print(f"Log file: {log_file}")

    input("Press Enter to start phase 2 (playback of the last phase)...")

    cmd = f"{args.exe} -playback {log_file} -logDuringPlayback -logFilenameExtra phase2"
    print(f"Phase 2: running `{cmd}`")
    os.system(cmd)

    log_file_randomized = log_dir.with_stem(log_dir.stem + "-randomized")
    dir = os.path.relpath(os.path.dirname(os.path.abspath(__file__)), os.getcwd())
    script = os.path.join(dir, "randomize_log.py")
    subprocess_args = [
        sys.executable, script, 
        "--input", log_file, 
        "--output", log_file_randomized, 
        "--sample", str(args.sample), 
        "--delay", str(args.hold)
    ]
    print("Phase 3: randomizing log...")
    subprocess.run(subprocess_args)
    print("done")

    for i in range(args.repeat):
        if args.repeat > 1:
            input(f"Press Enter to start phase 3 (playback in random order with teleporting) iteration {i + 1}...")
        else:
            input(f"Press Enter to start phase 3 (playback in random order with teleporting)...")
        extra = f"phase3-{i + 1}" if args.repeat > 1 else "phase3"
        cmd = f"{args.exe} -playback {log_file_randomized} -logDuringPlayback -logFilenameExtra {extra}"
        print(f"Phase 3: running `{cmd}`")
        os.system(cmd)
