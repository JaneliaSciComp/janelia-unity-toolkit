import argparse
import json
import os
import random

def read(input):
    with open(input, "r") as f:
        all = json.load(f)
        selected = [x for x in all if "attemptedTranslation" in x]
        return selected

def randomize(items, delay_secs, delay_frames):
    frame = 1
    time = 0
    result = []
    while len(items) > 0:
        i = random.randint(0, len(items) - 1)
        item = items.pop(i)
        item["timeSecs"] = time
        item["timeSecsAfterSplash"] = time
        item["frame"] = frame
        item["frameAfterSplash"] = frame
        result.append(item)
        frame += delay_frames
        time += delay_secs
    return result

def write(items, output):   
    with open(output, "w") as f:
        f.write("[\n")
        for i in range(len(items)):
            separator = "," if i < len(items) - 1 else ""
            item = json.dumps(items[i], indent=2)
            f.write(f"{item}{separator}\n")
        f.write("]\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", "-i", help="input log file (JSON)")
    parser.add_argument("--output", "-o", default=None, help="output randomized log file (JSON)")
    parser.add_argument("--sample", "-s", type=float, default=1, help="sampling stride (in seconds) for input")
    parser.add_argument("--delay", "-d", type=float, default=1, help="delay (i.e., hold, in seconds) for each output item")
    parser.add_argument("--fps", type=int, default=120, help="frames per second")
    args = parser.parse_args()

    output = args.output
    if not output:
        output = os.path.splitext(args.input)[0] + f"-randomized-delay{args.delay}.json"
    print(f"Using input: {args.input}")
    print(f"Using sample: every {args.sample} seconds")
    print(f"Using delay (per-sample hold): {args.delay} seconds")
    print(f"Using output: {output}")

    if not os.path.exists(args.input):
        print(f"Cannot find input {args.input}")
        sys.exit()
    
    original = read(args.input)
    sample_frames = round(args.sample * args.fps)
    sampled = [original[i] for i in range(0, len(original), sample_frames)]
    delay_secs = args.delay
    delay_frames = round(delay_secs * args.fps)
    randomized = randomize(sampled, delay_secs, delay_frames)
    write(randomized, output)
