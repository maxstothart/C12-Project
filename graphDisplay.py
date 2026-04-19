import matplotlib.pyplot as plt
import math
import numpy as np



def getSigmoid(accuracy, min=-10, max=10):
    YPoints = []
    point = min
    while point < max:
        YPoints += 1 / (1+math.e.__pow__(-point)), 
        point += accuracy

    return YPoints#np.array(YPoints)
def getTanH(accuracy, min=-10, max=10):
    YPoints = []
    point = min
    while point < max:
        YPoints += math.tanh(point), 
        point += accuracy

    return np.array(YPoints)
def getRELU(accuracy: float, min=-10, max=10):
    YPoints = []
    point = min
    while point < max:
        if point >= 0:
            YPoints += point,
        else:
            YPoints += 0, 
        point += accuracy
    return np.array(YPoints)
def getIndices(accuracy, min=-10, max=10):
    XPoints = []
    point = min
    while point < max:
        
        XPoints += point,
        point += accuracy
        
    return XPoints


fig, (ax1, ax2, ax3) = plt.subplots(3, sharex=True)
fig.subplots_adjust(hspace=0.3)
fig.suptitle('Activation Functions')
ax1.plot(np.linspace(-10, 10, 201), getSigmoid(0.1), 'tab:red')
ax1.set_title("Sigmoid: ")
ax2.plot(np.linspace(-10, 10, 201), getTanH(0.1), 'tab:green')
ax2.set_title("Hyperbolic Tan: ")
ax3.plot(np.linspace(-10, 10, 201), getRELU(0.1), 'tab:blue')
ax3.set_title("RELU: ")

ax1.axvline(x=0, color='black', linestyle='-',linewidth=1)
ax2.axvline(x=0, color='black', linestyle='-',linewidth=1)
ax3.axvline(x=0, color='black', linestyle='-',linewidth=1)

plt.show()