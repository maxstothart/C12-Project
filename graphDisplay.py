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
    graph.plot(np.linspace(min, max, int((max-min)/accuracy)), np.array(y), flags)

def graphFunctions(title, funcs, vertSpacing=0.3, shareX=True, shareY=False):
    colours = ("red", "green", "blue")#, "black")
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


if False:
    Functions = []
    Functions += ("1/(1+math.e**-x)", "Sigmoid: ", -10, 10, 0.1),
    Functions += ("math.tanh(x)", "Hyperbolic Tan: ", -10, 10, 0.1),
    Functions += ("x if x > 0 else 0", "ReLU: ", -10, 10, 0.1),

    graphFunctions("Activation Functions", Functions, 0.3, True, False)

if False:
    graphFunction(plt, "1/(1 + math.e**(-0.5*(x-20)))", 0, 40, 0.1)
    plt.show()

if False:
    Functions = []
    Functions += ("(x*10)**3", ".2", 0, 40, 0.1),
    Functions += ("1/(1+math.e**(-x-(1/6)*x**3))", ".2", 0, 40, 0.1),

    graphFunctions("Sigmoid Curves", Functions, 0.3, True, False)


def adjustable_shelf_curve(t, t_rise, t_shelf, t_fall, k):
    # Calculate key time boundary milestones
    t1 = t_rise
    t2 = t_rise + t_shelf
    t_end = t_rise + t_shelf + t_fall
    
    # Initialize output array with zeros
    y = np.zeros_like(t)
    
    # Condition 1: The Rising Sigmoid
    mask_rise = t < t1
    if np.any(mask_rise):
        center_rise = t_rise / 2
        y[mask_rise] = 1 / (1 + np.exp(-k * (t[mask_rise] - center_rise)))
        
    # Condition 2: The Constant Shelf
    mask_shelf = (t >= t1) & (t < t2)
    y[mask_shelf] = 1.0
    
    # Condition 3: The Falling Sigmoid
    mask_fall = (t >= t2) & (t <= t_end)
    if np.any(mask_fall):
        center_fall = t2 + (t_fall / 2)
        # Note the positive k in the exponent to invert the direction
        y[mask_fall] = 1 / (1 + np.exp(k * (t[mask_fall] - center_fall)))
        
    return y

if False:


    # --- Test run the configuration ---
    total_duration = 40.0
    t_space = np.linspace(0, total_duration, 1000)

    # Variables you can tweak interactively:
    rise_time = total_duration * 0.4  # How long it takes to reach the top
    shelf_time = total_duration * 0.1  # Length of time staying at the top
    fall_time = total_duration * 0.4   # How long it takes to drop back down
    steepness = 0.8   # The curve angle multiplier (try values from 0.5 to 10.0)


    # Generate curve data
    curve_values = adjustable_shelf_curve(t_space, rise_time, shelf_time, fall_time, steepness)

    # Plotting the visual response
    plt.figure(figsize=(10, 5))
    plt.plot(t_space, curve_values, label=f"Shelf Curve (Angle/k={steepness})", lw=2.5, color='teal')
    plt.axvline(x=rise_time, color='gray', linestyle='--', alpha=0.5, label='Shelf Starts')
    plt.axvline(x=rise_time + shelf_time, color='gray', linestyle='-.', alpha=0.5, label='Shelf Ends')
    plt.title("Adjustable Sigmoidal Shelf Curve")
    plt.xlabel("Time (seconds)")
    plt.ylabel("Amplitude")
    plt.grid(True, alpha=0.3)
    plt.legend()
    plt.ylim(-0.05, 1.05)
    plt.show()

if True:
    H = 100
    W = 200
    X = 40
    Y = 50
    Functions = []
    Functions += (f"math.tan(3.1415/360*x)*({H-Y})+{X}", "0-90", 0, 90, 1),
    Functions += (f"math.tan(3.1415/360*x)*({W-X})+{H-Y}", "0-90", 90, 180, 1),
    Functions += (f"math.tan(3.1415/360*x)*({Y})+{W-X}", "0-90", 180, 270, 1),
    Functions += (f"math.tan(3.1415/360*x)*({X})+{Y}", "0-90", 270, 360, 1),
    graphFunctions("Box SDF", Functions)