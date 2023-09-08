# Simplifies a log file by combining entries having the same time stamp (i.e., frame).

# For example, say the log file is the following:
# [
# {
#     "timeSecs": 0.88304,
#     "frame": 100.0,
#     "timeSecsAfterSplash": 0.88304,
#     "frameAfterSplash": 100.0,
#     "a": 1
# },
# {
#     "timeSecs": 0.88304,
#     "frame": 100.0,
#     "timeSecsAfterSplash": 0.88304,
#     "frameAfterSplash": 100.0,
#     "b": 1.0
# },
# {
#     "timeSecs": 0.88304,
#     "frame": 100.0,
#     "timeSecsAfterSplash": 0.88304,
#     "frameAfterSplash": 100.0,
#     "c": {
#         "x": 1.0,
#         "y": 1.1,
#         "z": 1.2
#     }
# },
# {
#     "timeSecs": 0.88612,
#     "frame": 101.0,
#     "timeSecsAfterSplash": 0.88612,
#     "frameAfterSplash": 101.0,
#     "a": 2
# }
# ]

# The simplified version would be:
# [
# {
#     "timeSecs": 0.88304,
#     "frame": 100.0,
#     "timeSecsAfterSplash": 0.88304,
#     "frameAfterSplash": 100.0,
#     "a": 1,
#     "b": 1.0,
#     "c": {
#         "x": 1.0,
#         "y": 1.1,
#         "z": 1.2
#     }
# },
# {
#     "timeSecs": 0.88612,
#     "frame": 101.0,
#     "timeSecsAfterSplash": 0.88612,
#     "frameAfterSplash": 101.0,
#     "a": 2
# }
# ]

import argparse
import json
import os
import sys

def header_keys():
    return ["timeSecs", "frame", "timeSecsAfterSplash", "frameAfterSplash"]

def skip(json, skippable):
    for key in json.keys():
        if key in skippable:
            return True
    return False

def headers_match(json1, json2):
    for key in header_keys():
        if (key not in json1) or (key not in json2) or (json1[key] != json2[key]):
            return False
    return True

def matches_exactly(json1, json2):
    if len(json1.keys()) != len(json2.keys()):
        return False
    for key1, value1 in json1.items():
        if key1 not in json2 or json2[key1] != value1:
            return False
    return True

def mergeable(json1, json2):
    if matches_exactly(json1, json2):
        return False
    keys1 = [key for key in json1.keys() if key not in header_keys()]
    keys2 = [key for key in json2.keys() if key not in header_keys()]
    for key1 in keys1:
        if key1 in keys2:
            return False
    for key2 in keys2:
        if key2 in keys1:
            return False
    return True

def merge_into(json1_merged, json2):
    for key2, val2 in json2.items():
        json1_merged[key2] = val2

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", "-i", help="path to original log file")
    parser.add_argument("--output", "-o", help="path for output, merged log file")
    parser.set_defaults(skip=["meshGameObjectPath"])
    parser.add_argument("--skip", "-s", nargs="+", help="skip merging of items containing these keys")
    parser.set_defaults(limit=0)
    parser.add_argument("--limit", type=int, help="limit the output to this many items (0 means no limit)")
    args = parser.parse_args()

    print(f"Using input: {args.input}")
    output = args.output
    if output == None:
        root, ext = os.path.splitext(args.input)
        output = root + "-merged" + ext
    print(f"Using output: {output}")
    print(f"Skipping merging of records containing: {args.skip}")

    with open(args.input, "r") as f:
        json_orig = json.load(f)
        json_result = []
        n = len(json_orig)
        i1 = 0
        
        decile = n // 10
        while i1 < n:
            if len(json_result) % decile == 0:
                print(f"{round(i1 / n * 100)}%")

            json1 = json_orig[i1]
            json1_merged = json1.copy()
            i2 = i1 + 1

            if skip(json1, args.skip):
                i1 = i2
            else:
                while i2 < len(json_orig):
                    json2 = json_orig[i2]
                    if not headers_match(json1_merged, json2):
                        break
                    if skip(json2, args.skip):
                        break
                    if mergeable(json1_merged, json2):
                        merge_into(json1_merged, json2)
                    i2 += 1

                i1 = i2

            json_result.append(json1_merged)

            if args.limit > 0 and i1 > args.limit:
                break
    
    with open(output, "w") as f:
        json.dump(json_result, f, indent=2)
