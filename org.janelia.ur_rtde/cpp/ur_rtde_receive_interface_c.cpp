#include <ur_rtde/rtde_receive_interface.h>
#include <set>

namespace {
    std::set<ur_rtde::RTDEReceiveInterface*>& receiveInterfaces()
    {
        static std::set<ur_rtde::RTDEReceiveInterface*> interfaces;
        return interfaces;
    }

    bool isValidReceiveInterface(ur_rtde::RTDEReceiveInterface* interface)
    {
        if (receiveInterfaces().find(interface) != receiveInterfaces().end()) {
            return true;
        } else {
            std::cerr << "Error in org.janelia.ur_rtde: Invalid Ur_rtde ReceiveInterface";
            return false;
        }
    }
}

extern "C" {
    ur_rtde::RTDEReceiveInterface*
    Ur_rtde_RTDEReceiveInterface_new(const char* ip = "172.17.0.2", bool verbose = false)
    {
        float frequency = -1;
        std::vector<std::string> variables = {};
        try {
            ur_rtde::RTDEReceiveInterface* obj = 
                new ur_rtde::RTDEReceiveInterface(ip, frequency, variables, verbose);
            receiveInterfaces().insert(obj);
            return obj;
        }
        catch (...) {
            std::cerr << "Error in org.janelia.ur_rtde: Could not create Ur_rtde ReceiveInterface";
            return 0;
        }
    }

    void 
    Ur_rtde_RTDEReceiveInterface_delete(ur_rtde::RTDEReceiveInterface* obj)
    {
        auto it = receiveInterfaces().find(obj);
        if (it != receiveInterfaces().end()) {
            receiveInterfaces().erase(it);
            delete obj;
        }
    }

    bool
    Ur_rtde_RTDEReceiveInterface_isConnected(ur_rtde::RTDEReceiveInterface* obj)
    {
        if (!isValidReceiveInterface(obj)) {
            return false;
        }
        return obj->isConnected();
    }

    bool
    Ur_rtde_RTDEReceiveInterface_getActualQ(ur_rtde::RTDEReceiveInterface* obj,
                                            float* a0, float* a1, float* a2,
                                            float* a3,  float* a4, float* a5)
    {
        if (!isValidReceiveInterface(obj)) {
            *a0 = *a1 = *a2 = *a3 = *a4 = *a5 = 0;
            return false;
        }
        std::vector<double> pose = obj->getActualQ();
        *a0 = float(pose[0]);
        *a1 = float(pose[1]);
        *a2 = float(pose[2]);
        *a3 = float(pose[3]);
        *a4 = float(pose[4]);
        *a5 = float(pose[5]);
        return true;
    }

    bool
    Ur_rtde_RTDEReceiveInterface_getActualTCPPose(ur_rtde::RTDEReceiveInterface* obj,
                                                  float* x, float* y, float* z,
                                                  float* rx, float* ry, float* rz)
    {
        if (!isValidReceiveInterface(obj)) {
            *x = *y = *z = *rx = *ry = *rz = 0;
            return false;
        }
        std::vector<double> pose = obj->getActualTCPPose();
        *x = float(pose[0]);
        *y = float(pose[1]);
        *z = float(pose[2]);
        *rx = float(pose[3]);
        *ry = float(pose[4]);
        *rz = float(pose[5]);
        return true;
    }

    bool
    Ur_rtde_RTDEReceiveInterface_isProtectiveStopped(ur_rtde::RTDEReceiveInterface* obj)
    {
        if (!isValidReceiveInterface(obj)) {
            return false;
        }
        return obj->isProtectiveStopped();
    }

    bool
    Ur_rtde_RTDEReceiveInterface_isEmergencyStopped(ur_rtde::RTDEReceiveInterface* obj)
    {
        if (!isValidReceiveInterface(obj)) {
            return false;
        }
        return obj->isEmergencyStopped();
    }
}
