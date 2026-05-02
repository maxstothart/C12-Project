import matplotlib.pyplot as plt
import math
import numpy as np



def graphFunction(graph, func: str, min, max, accuracy: float, flags:str = ""):
    x = min
    y = []
    Func = eval("lambda x: "+func)
    while x < max:
        y += Func(x),
        x += accuracy
    graph.plot(np.linspace(min, max, int((max-min)/accuracy+1)), np.array(y), flags)

def graphFunctions(title, funcs, vertSpacing=0.3, shareX=True, shareY=False):
    colours = ("red", "green", "blue", "black")
    #(func, title, min, max, accuracy)
    fig, ax = plt.subplots(len(funcs), sharex=shareX, sharey=shareY)
    fig.subplots_adjust(hspace=vertSpacing)
    fig.suptitle(title)

    for i in range(0, len(ax)):
        ax[i].set_title(funcs[i][1])
        graphFunction(ax[i], funcs[i][0], funcs[i][2], funcs[i][3], funcs[i][4], f"tab:{colours[i%len(colours)]}")

    for ax in ax:
        ax.axvline(x=0, color='black', linestyle='-',linewidth=1)
    plt.show()

if False:
    Functions = []
    Functions += ("x**2", "X^2", -10, 10, 0.1),
    Functions += ("x**2+2*x", "X^2+2x", -10, 10, 0.1),
    graphFunctions("X Perm", Functions, 0.3, True, False)


if True:
    Functions = []
    Functions += ("1/(1+math.e**-x)", "Sigmoid: ", -10, 10, 0.1),
    Functions += ("math.tanh(x)", "Hyperbolic Tan: ", -10, 10, 0.1),
    Functions += ("x if x > 0 else 0", "ReLU: ", -10, 10, 0.1),

    graphFunctions("Activation Functions", Functions, 0.3, True, False)