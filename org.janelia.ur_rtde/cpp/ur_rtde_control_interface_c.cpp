#include <ur_rtde/rtde_control_interface.h>
#include <chrono>
#include <set>

namespace {
    std::set<ur_rtde::RTDEControlInterface*>& controlInterfaces()
    {
        static std::set<ur_rtde::RTDEControlInterface*> interfaces;
        return interfaces;
    }

    bool isValidControlInterface(ur_rtde::RTDEControlInterface* interface)
    {
        if (controlInterfaces().find(interface) != controlInterfaces().end()) {
            return true;
        } else {
            std::cerr << "Error in org.janelia.ur_rtde: Invalid Ur_rtde ControlInterface";
            return false;
        }
    }
}

extern "C" {
    ur_rtde::RTDEControlInterface*
    Ur_rtde_RTDEControlInterface_new(const char* ip, bool verbose)
    {
        float frequency = -1;
        uint16_t flags = ur_rtde::RTDEControlInterface::FLAGS_DEFAULT;
        if (verbose) {
            flags |= ur_rtde::RTDEControlInterface::FLAG_VERBOSE;
        }
        try {
            ur_rtde::RTDEControlInterface* obj = 
                new ur_rtde::RTDEControlInterface(ip, frequency, flags);
            controlInterfaces().insert(obj);
            return obj;
        }
        catch (...) {
            std::cerr << "Error in org.janelia.ur_rtde: Could not create Ur_rtde ControlInterface";
            return 0;
        }
    }

    void 
    Ur_rtde_RTDEControlInterface_delete(ur_rtde::RTDEControlInterface* obj)
    {
        auto it = controlInterfaces().find(obj);
        if (it != controlInterfaces().end()) {
            controlInterfaces().erase(it);
            delete obj;
        }
    }

    long
    Ur_rtde_RTDEControlInterface_initPeriod(ur_rtde::RTDEControlInterface* obj)
    {
        if (!isValidControlInterface(obj)) {
            return 0;
        }
        auto tp = obj->initPeriod();
        auto ms = std::chrono::time_point_cast<std::chrono::milliseconds>(tp);
        auto value = ms.time_since_epoch();
        long duration = value.count();
        return duration;
    }

    void
    Ur_rtde_RTDEControlInterface_waitPeriod(ur_rtde::RTDEControlInterface* obj, long tCycleStart)
    {
        if (isValidControlInterface(obj)) {
            std::chrono::milliseconds duration(tCycleStart);
            std::chrono::time_point<std::chrono::steady_clock> tp(duration);
            obj->waitPeriod(tp);
        }
    }

    void
    Ur_rtde_RTDEControlInterface_stopScript(ur_rtde::RTDEControlInterface* obj)
    {
        if (isValidControlInterface(obj)) {
            obj->stopScript();
        }
    }

    void
    Ur_rtde_RTDEControlInterface_stopL(ur_rtde::RTDEControlInterface* obj, float a, bool asynchronous)
    {
        if (isValidControlInterface(obj)) {
            obj->stopL(a, asynchronous);
        }
    }

    void
    Ur_rtde_RTDEControlInterface_stopJ(ur_rtde::RTDEControlInterface* obj, float a, bool asynchronous)
    {
        if (isValidControlInterface(obj)) {
            obj->stopJ(a, asynchronous);
        }
    }

    bool 
    Ur_rtde_RTDEControlInterface_moveJ(ur_rtde::RTDEControlInterface* obj,
                                       float r0, float r1, float r2, float r3, float r4, float r5,
                                       float speed, float acceleration, bool asynchronous)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        std::vector<double> pose{r0, r1, r2, r3, r4, r5};
        return obj->moveJ(pose, speed, acceleration, asynchronous);
    }

    bool 
    Ur_rtde_RTDEControlInterface_moveJ_IK(ur_rtde::RTDEControlInterface* obj,
                                          float r0, float r1, float r2, float r3, float r4, float r5,
                                          float speed, float acceleration, bool asynchronous)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        std::vector<double> pose{r0, r1, r2, r3, r4, r5};
        return obj->moveJ_IK(pose, speed, acceleration, asynchronous);
    }

    bool 
    Ur_rtde_RTDEControlInterface_moveL(ur_rtde::RTDEControlInterface* obj,
                                       float x, float y, float z,
                                       float rx, float ry, float rz,
                                       float speed, float acceleration, bool asynchronous)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        std::vector<double> pose{x, y, z, rx, ry, rz};
        return obj->moveL(pose, speed, acceleration, asynchronous);
    }

    bool 
    Ur_rtde_RTDEControlInterface_moveL_FK(ur_rtde::RTDEControlInterface* obj,
                                          float x, float y, float z,
                                          float rx, float ry, float rz,
                                          float speed, float acceleration, bool asynchronous)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        std::vector<double> pose{x, y, z, rx, ry, rz};
        return obj->moveL_FK(pose, speed, acceleration, asynchronous);
    }

    bool
    Ur_rtde_RTDEControlInterface_jogStart(ur_rtde::RTDEControlInterface* obj,
                                          float s0, float s1, float s2,
                                          float s3, float s4, float s5,
                                          bool tool)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        std::vector<double> speeds{s0, s1, s2, s3, s4, s5};
        int feature = tool ? ur_rtde::RTDEControlInterface::FEATURE_TOOL : ur_rtde::RTDEControlInterface::FEATURE_BASE;
        return obj->jogStart(speeds, feature);
    }

    bool
    Ur_rtde_RTDEControlInterface_jogStop(ur_rtde::RTDEControlInterface* obj)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }
        return obj->jogStop();
    }

    bool
    Ur_rtde_RTDEControlInterface_movePath(ur_rtde::RTDEControlInterface* obj,
                                          int count, int moveTypes[], int positionTypes[], int parametersCounts[],
                                          int parametersCountTotal, float parameters[], bool asynchronous)
    {
        if (!isValidControlInterface(obj)) {
            return false;
        }

        ur_rtde::Path path;
        int j = 0;
        for (int i = 0; i < count; ++i) {
            ur_rtde::PathEntry::eMoveType moveType;
            switch (moveTypes[i]) {
                case 0: 
                    moveType = ur_rtde::PathEntry::eMoveType::MoveJ;
                    break;
                case 1: 
                    moveType = ur_rtde::PathEntry::eMoveType::MoveL;
                    break;
                case 2: 
                    moveType = ur_rtde::PathEntry::eMoveType::MoveP;
                    break;
                case 3: 
                    moveType = ur_rtde::PathEntry::eMoveType::MoveC;
                    break;
            }

            ur_rtde::PathEntry::ePositionType posType;
            posType = (positionTypes[i] == 0) ? 
                ur_rtde::PathEntry::ePositionType::PositionTcpPose : 
                ur_rtde::PathEntry::ePositionType::PositionJoints;

            std::vector<double> params;
            for (int k = 0; k < parametersCounts[i]; ++k) {
                params.push_back(parameters[j++]);
            }

            path.addEntry({moveType, posType, params});
        }

        return obj->movePath(path, asynchronous);
    }

    int
    Ur_rtde_RTDEControlInterface_getAsyncOperationProgress(ur_rtde::RTDEControlInterface* obj)
    {
        if (!isValidControlInterface(obj)) {
            return 0;
        }
        return obj->getAsyncOperationProgress();
    }
}
