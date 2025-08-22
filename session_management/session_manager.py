# Usage:
# python session-manager.py -ip /path/to/paradigm_input.json -it /path/to/trial_input.json
# python session-manager.py -ip /path/to/paradigm_input.json -it /path/to/trial_input.json --start 3
# python session-manager.py -ip /path/to/paradigm_input.json -it /path/to/trial_input.json --dry-run

import argparse
import json
import os
import platform
import time
import sys


def remove_comments(file):
    output = ""
    with open(file) as f:
        for line in f:
            line_stripped = line.lstrip()
            if line_stripped.startswith("#") or line_stripped.startswith("//"):
                # Replace a comment line with a blank line, so the line count stays the same in error messages.
                output += "\n"
            else:
                output += line
    return output


def get_sessions(json_all):
    return json_all["sessions"]


def normalize_key(key):
    return key.lower().replace("_", "")


def absolutize_path(path):
    if os.path.isabs(path):
        return path
    else:
        return os.path.join(os.path.dirname(__file__), path)


def get_key_value(json_session, json_all, is_key_func):
    val = None
    for key in json_all:
        if is_key_func(key):
            val = json_all[key]
    for key in json_session:
        if is_key_func(key):
            session_val = json_session[key]
            # if json_all[key] and json_session[key] are both dicts
            # overwrite dictionary values with those from json_session version
            if isinstance(session_val, dict) and isinstance(val, dict):
                val = val.copy()
                val.update(session_val)
            # if either the json_all or the json_session value are not dictionaries
            # simply set the return value to the session value
            else:
                val = session_val
    return val


def is_executable_key(key):
    key = normalize_key(key)
    return key.startswith("exe") or key.startswith("bin") or key.startswith("prog")


def get_executable(paradigm_json_session, paradigm_json_all):
    exe = get_key_value(paradigm_json_session, paradigm_json_all, is_executable_key)

    if not os.path.exists(exe):
        if platform.system() == "Windows":
            if not exe.endswith(".lnk"):
                exe2 = exe + ".lnk"
                if os.path.exists(exe2):
                    exe = exe2
            if not exe.endswith(".exe"):
                exe2 = exe + ".exe"
                if os.path.exists(exe2):
                    exe = exe2

    return absolutize_path(exe)


def is_log_filename_extra_key(key):
    key = normalize_key(key)
    return key.startswith("logfilenameextra")


def get_log_filename_extra(json_session, json_all):
    return get_key_value(json_session, json_all, is_log_filename_extra_key) or ""


def is_log_header_key(key):
    key = normalize_key(key)
    return key.startswith("logheader")


def get_log_header(json_session, json_all):
    return get_key_value(json_session, json_all, is_log_header_key) or ""


def is_log_dir_key(key):
    key = normalize_key(key)
    return key.startswith("logdir")


def get_log_dir(paradigm_json_session, paradigm_json_all):
    log_dir = get_key_value(paradigm_json_session, paradigm_json_all, is_log_dir_key)
    return absolutize_path(log_dir)


def is_session_parameters_key(key):
    key = normalize_key(key)
    return key.startswith("sessionparam")


def get_session_parameters(paradigm_json_session, paradigm_json_all):
    return get_key_value(
        paradigm_json_session, paradigm_json_all, is_session_parameters_key
    )


def is_pause_key(key):
    key = normalize_key(key)
    return key.startswith("pause")


def get_pause(paradigm_json_session, paradigm_json_all):
    return get_key_value(paradigm_json_session, paradigm_json_all, is_pause_key)


def process_log_dir(log_dir, dry_run):
    if not os.path.exists(log_dir):
        child = log_dir[:-1] if log_dir.endswith("/") else log_dir
        parent = os.path.dirname(child)
        if os.path.exists(parent):
            print("Creating log directory: {}".format(log_dir))
            if not dry_run:
                os.mkdir(log_dir)
        else:
            print(
                "Warning: log directory {} does not exists, nor does the parent directory".format(
                    child
                )
            )


def process_log_filename_extra(log_filename_extra, cmd, dry_run):
    if log_filename_extra:
        cmd += " -logFilenameExtra {}".format(log_filename_extra)
        print("Appending to the log file name: {}".format(log_filename_extra))
    return cmd


def process_log_header(log_header, cmd, dry_run):
    if log_header:
        if log_dir:
            log_header_file = os.path.join(log_dir, "logHeader.txt")
            print("Writing to {}:\n{}".format(log_header_file, log_header))
            if not dry_run:
                if os.path.exists(log_header_file):
                    os.remove(log_header_file)
                with open(log_header_file, "w") as f:
                    f.write(log_header + "\n")
            cmd += " -addLogHeader"
    return cmd


def process_session_params(session_params, cmd, dry_run):
    if session_params:
        if log_dir:
            session_params_file = os.path.join(log_dir, "SessionParameters.json")
            pretty = json.dumps(session_params, indent=4)
            print("Writing to {}:\n{}".format(session_params_file, pretty))
            if not dry_run:
                if os.path.exists(session_params_file):
                    os.remove(session_params_file)
                with open(session_params_file, "w") as f:
                    f.write(pretty + "\n")
    return cmd


def process_pause(pause_secs, cmd, dry_run):
    if pause_secs:
        print("Pausing {} secs".format(pause_secs))
        if not dry_run:
            time.sleep(pause_secs)
    return cmd


def process_missing_exe(exe):
    if platform.system() == "Windows":
        if not exe.endswith(".exe") and not exe.endswith(".lnk"):
            print("The executable may need an explicit '.exe' or '.lnk' extension")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--input-paradigm",
        "-ip",
        dest="input_paradigm_json_file",
        help="path to the JSON file describing the paradigm(s)s",
    )
    parser.add_argument(
        "--input-trial",
        "-it",
        dest="input_trial_json_file",
        help="path to the JSON file describing the trial(s)",
    )
    parser.set_defaults(start=1)
    parser.add_argument(
        "--start",
        "-s",
        type=int,
        dest="start",
        help="session to start at (1 is the first)",
    )
    parser.set_defaults(dry_run=False)
    parser.add_argument(
        "--dry-run",
        "-dry-run",
        dest="dry_run",
        action="store_true",
        help="only print what would be run",
    )
    args = parser.parse_args()

    print("Using input paradigm JSON file: {}".format(args.input_paradigm_json_file))
    print("Using input trial JSON file: {}".format(args.input_trial_json_file))
    print("Dry run: {}".format(args.dry_run))

    paradigm_json_all = json.loads(remove_comments(args.input_paradigm_json_file))
    trial_json_all = json.loads(remove_comments(args.input_trial_json_file))

    paradigm_json_sessions = get_sessions(paradigm_json_all)
    trial_json_sessions = get_sessions(trial_json_all)

    if len(paradigm_json_sessions) != len(trial_json_sessions):
        print(
            "the input-paradigm and input-trial files must have the same number of sessions!"
        )
        print(f"the input-paradigm file has {len(paradigm_json_sessions)} sessions")
        print(f"the input-trial file has {len(trial_json_sessions)} sessions")
        sys.exit()

    for i in range(args.start - 1, len(paradigm_json_sessions)):
        print(f"Session {i+1} / {len(paradigm_json_sessions)}: ")

        paradigm_json_session = paradigm_json_sessions[i]
        trial_json_session = trial_json_sessions[i]

        exe = get_executable(paradigm_json_session, paradigm_json_all)
        cmd = exe

        log_dir = get_log_dir(paradigm_json_session, paradigm_json_all)
        process_log_dir(log_dir, args.dry_run)

        paradigm_log_filename_extra = get_log_filename_extra(
            paradigm_json_session, paradigm_json_all
        )
        trial_log_filename_extra = get_log_filename_extra(
            trial_json_session, trial_json_all
        )
        full_log_filename_extra = paradigm_log_filename_extra + trial_log_filename_extra
        cmd = process_log_filename_extra(full_log_filename_extra, cmd, args.dry_run)

        paradigm_log_header = get_log_header(paradigm_json_session, paradigm_json_all)
        trial_log_header = get_log_header(trial_json_session, trial_json_all)
        full_log_header = paradigm_log_header + "\n" + trial_log_header
        cmd = process_log_header(full_log_header, cmd, args.dry_run)

        session_params = get_session_parameters(
            paradigm_json_session, paradigm_json_all
        )
        cmd = process_session_params(session_params, cmd, args.dry_run)

        pause_secs = get_pause(paradigm_json_session, paradigm_json_all)
        cmd = process_pause(pause_secs, cmd, args.dry_run)

        print(cmd)

        if not os.path.exists(exe):
            print("Cannot find executable {}".format(exe))
            process_missing_exe(exe)
            print("Skipping")
            continue

        if not args.dry_run:
            os.system(cmd)

        print("")

    print("Done")
