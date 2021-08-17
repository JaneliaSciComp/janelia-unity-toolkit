# Generates simple messages that can be read by ExampleReadingSocket.cs.
# Messages can be in either JSON format (with a newline terminator)
# or an ad hoc format (with a 'J' character header).
# Messages can be sent using either UDP or TCP.

import argparse
import math
import datetime
import functools
import random
import socket
import sys
import time

def message(i, args):
    global scale

    # Position in world space.
    posX = (4 * (1 - abs(0.5 - i / (args.count - 1))) - 3) * args.scale
    theta = i / args.count * 4 * math.pi
    posZ = math.sin(theta) * args.scale

    # Rotation (euler angles) in degrees.
    half = args.count / 2
    j = i % half
    rotY = 90 + (2 * (1 - abs(0.5 - j / (half - 1))) - 1) * 180
    rotY = rotY if (i < half) else 180 - rotY

    # Time in milliseconds since the Unix epoch.
    timestampMs = int(time.time() * 1000)

    if args.useJson:
        # Test how the reader handles two types of messages, distinguished by a `type` field.
        if i % int(half) == 0:
            scale = i // int(half) + 1
            msg = '{{ "type": "scale", "timestampMs": {}, "scale": {} }}\n'.format(timestampMs, scale)
        else:
            msg = '{{ "type": "pose", "timestampMs": {}, "pos": {{ "x" : {}, "y": 0.0, "z": {} }}, "rot": {{ "x": 0.0, "y": {}, "z": 0.0 }} }}\n'.format(timestampMs, posX, posZ, rotY)
    else:
        msgNumeric = [timestampMs, posX, 0.0, posZ, 0.0, rotY, 0.0]
        msg = functools.reduce(lambda a, b: a + " " + str(b), msgNumeric, "J")
        msg += " "

    return msg

if __name__ == "__main__":
    print("[{}] Server starting".format(datetime.datetime.now()))

    argv = sys.argv
    if "--" not in argv:
        argv = []
    else:
        argv = argv[argv.index("--") + 1:]

    parser = argparse.ArgumentParser(argv)
    parser.set_defaults(host="127.0.0.1")
    parser.add_argument("--addr", "-a", dest="host", help="socket host")
    parser.set_defaults(port=2000)
    parser.add_argument("--port", "-p", type=int, dest="port", help="socket port")
    parser.set_defaults(useUDP=True)
    parser.add_argument("--tcp", "-tcp", dest="useUDP", action="store_false", help="use TCP instead of UDP for the socket protocol")
    parser.set_defaults(count=2000)
    parser.add_argument("--count", "-c", type=int, dest="count", help="total message count for the cycle")
    parser.set_defaults(cycles=2)
    parser.add_argument("--cycles", "-cy", type=int, dest="cycles", help="numer of cycle")
    # A 240 Hz frame rate is 1 / 240 s between frames, or 0.004167 s or about 4 ms between frames.
    parser.set_defaults(delayMs=4)
    parser.add_argument("--delay", "-d", type=int, dest="delayMs", help="delay between messages (msec)")
    parser.set_defaults(scale=5)
    parser.add_argument("--scale", "-s", type=float, dest="scale", help="scale for the default [-1, 1] size")
    parser.set_defaults(timeout=30)
    parser.add_argument("--timeout", "-t", type=int, dest="timeout", help="server listening timeout (sec)")
    parser.set_defaults(noisePercentage=0.1)
    parser.add_argument("--noise", "-no", type=float, dest="noisePercentage", help="noise percentage of actual value")
    parser.set_defaults(useJson=False)
    parser.add_argument("--json", "-j", dest="useJson", action="store_true", help="use messages in JSON")
    args = parser.parse_args()

    socketType = socket.SOCK_DGRAM if args.useUDP else socket.SOCK_STREAM

    with socket.socket(socket.AF_INET, socketType) as sock:
        if args.useUDP:
            print("[{}] Server will send to {}".format(datetime.datetime.now(), (args.host, args.port)))

            for i in range(args.cycles):
                print("[{}] Cycle {} of {} ({:.2f}%)".format(datetime.datetime.now(), i, args.cycles, (i / args.cycles) * 100))

                for j in range(args.count):
                    msg = message(j, args)
                    sock.sendto(msg.encode('utf-8'), (args.host, args.port))
                    time.sleep(args.delayMs / 1000)

                print("[{}] Server done with {} messages".format(datetime.datetime.now(), args.count))

        else:
            sock.bind((args.host, args.port))
            sock.settimeout(args.timeout)
            print("[{}] Server listening".format(datetime.datetime.now()))
            sock.listen()
            conn, addr = sock.accept()
            with conn:
                print("[{}] Server connected, address {}".format(datetime.datetime.now(), addr))

                for i in range(args.cycles):
                    print("[{}] Cycle {} of {} ({:.2f}%)".format(datetime.datetime.now(), i, args.cycles, (i / args.cycles) * 100))

                    for j in range(args.count):
                        msg = message(j, args)
                        conn.sendall(msg.encode('utf-8'))
                        time.sleep(args.delayMs / 1000)

                    print("[{}] Server done with {} messages".format(datetime.datetime.now(), args.count))
