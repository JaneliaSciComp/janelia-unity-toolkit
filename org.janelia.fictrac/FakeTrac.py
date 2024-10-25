# Generates simple messages using the FicTrac protocol and sends the over a socket.
# http://rjdmoore.net/fictrac
# For testing an application that listens to FicTrac messages.
# Supports a socket using either UDP or TCP, with UDP being the default, to match
# the approach of the real FicTrac code as of late 2020.

import argparse
import math
import datetime
import functools
import random
import socket
import sys
import time

integratedHeading = 0
integratedX = 0
integratedY = 0

def message(i, args):
    global integratedHeading
    global integratedX, integratedY

    # FicTrac format:
    # https://github.com/rjdmoore/fictrac/blob/master/doc/data_header.txt
    # COL     PARAMETER                       DESCRIPTION
    # 1       frame counter                   Corresponding video frame (starts at #1).
    # 6-8     delta rotation vector (lab)     Change in orientation since last frame,
    #                                         represented as rotation angle/axis (radians)
    #                                         in laboratory coordinates.
    # 15-16   integrated x/y position (lab)   Integrated x/y position (radians) in
    #                                         laboratory coordinates. Scale by sphere
    #                                         radius for true position.
    # 17      integrated animal heading (lab) Integrated heading orientation (radians) of
    #                                         the animal in laboratory coordinates. This
    #                                         is the direction the animal is facing.
    # 22      timestamp                       Either position in video file (ms) or frame
    #                                         capture time (ms since epoch).

    msgNumeric = [0 for _ in range(26+1)]
    frame = i + 1
    msgNumeric[1] = frame

    # Produces movement in a circle on a plane, with a little jitter.
    forward = args.translationRate
    forward += forward * random.uniform(-args.noisePercentage, args.noisePercentage)
    rotationRad = args.rotationRate / (2 * math.pi)
    rotationRad += rotationRad * random.uniform(-args.noisePercentage, args.noisePercentage)

    if args.oscillating:
        p = 200
        d = (i + p/2) // p
        if d % 2 == 1:
            rotationRad *= -1

    d = i // 60
    if args.free and d % 2 == 1:
        rotationRad *= 1 # 1.2
    elif args.stepped:
        d = i // 6
        if d % 2 == 1:
            forward = 0
            rotationRad = 0

    # https://www.researchgate.net/figure/Visual-output-from-the-FicTrac-software-see-supplementary-video-a-A-segment-of-the_fig2_260044337
    # Rotation about `a_x` is sideways translation.
    # Rotation about `a_y` is forwards/backwards translation.
    # Rotation about `a_z` is heading change.
    # The client should consider positive `x` to be the forward direction,
    # and positive `y` to be the up direction (the normal for the plane containing the motion).
    msgNumeric[6] = 0
    msgNumeric[7] = forward
    msgNumeric[8] = rotationRad

    # Empirically, it seems that the heading change SHOULD be negated here, to match the real FicTrac.
    integratedHeading -= rotationRad
    msgNumeric[17] = integratedHeading

    changeX = math.cos(-integratedHeading) * forward
    changeY = math.sin(-integratedHeading) * forward

    # In the FicTrac `data_header.txt`, for the "integrated x/y position (in radians) in lab coordinates",
    # the receiver is to "scale by sphere radius for true position", so do the inverse of scaling by
    # that radius here.
    changeX /= args.radius
    changeY /= args.radius

    integratedX += changeX
    integratedY += changeY
    msgNumeric[15] = integratedX
    msgNumeric[16] = integratedY
 
    # Time (in milliseconds) since the Unix epoch.
    timestampMs = int(time.time() * 1000)
    msgNumeric[22] = timestampMs
    
    msgNumeric[23] = frame
    msgNumeric[24] = args.delayMs

    msg = functools.reduce(lambda a, b: a + ", " + str(b), msgNumeric[1:], "FT")
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
    # A 240 Hz frame rate is 1 / 240 s between frames, or 0.004167 s or about 4 ms between frames.
    parser.set_defaults(delayMs=4)
    parser.add_argument("--delay", "-d", type=int, dest="delayMs", help="delay between messages (msec)")
    parser.set_defaults(timeout=30)
    parser.add_argument("--timeout", "-t", type=int, dest="timeout", help="server listening timeout (sec)")
    parser.set_defaults(count=2000)
    parser.add_argument("--count", "-c", type=int, dest="count", help="total message count")
    parser.set_defaults(translationRate=0.001)
    parser.add_argument("--trans", "-tr", type=float, dest="translationRate", help="base translation rate")
    parser.set_defaults(rotationRate=0.05)
    parser.add_argument("--rot", "-ro", type=float, dest="rotationRate", help="base rotate rate")
    parser.set_defaults(noisePercentage=0.1)
    parser.add_argument("--noise", "-no", type=float, dest="noisePercentage", help="noise percentage of actual value")
    parser.set_defaults(free=False)
    parser.add_argument("--free", "-fr", dest="free", action="store_true", help="add occasional chaos to test thresholding")
    parser.set_defaults(stepped=False)
    parser.add_argument("--stepped", "-st", dest="stepped", action="store_true", help="produce motion in a stepping pattern")
    parser.set_defaults(oscillating=False)
    parser.add_argument("--oscillate", "-os", dest="oscillating", action="store_true", help="produce oscilating rotation")
    parser.set_defaults(radius=1.0)
    parser.add_argument("--rad", "-r", type=float, dest="radius", help="trackball radius (for integrated x, y only)")
    args = parser.parse_args()

    socketType = socket.SOCK_DGRAM if args.useUDP else socket.SOCK_STREAM

    with socket.socket(socket.AF_INET, socketType) as sock:
        if args.useUDP:
            print("[{}] Server will send to {}".format(datetime.datetime.now(), (args.host, args.port)))

            for i in range(args.count):
                msg = message(i, args)
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

                for i in range(args.count):
                    msg = message(i, args)
                    conn.sendall(msg.encode('utf-8'))
                    time.sleep(args.delayMs / 1000)

                print("[{}] Server done with {} messages".format(datetime.datetime.now(), args.count))
