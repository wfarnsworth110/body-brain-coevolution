from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List
import random
import numpy as np
import json
import csv
import os
from deap import base, creator, tools

app = FastAPI(title="Body-Brain Co-evolution API")

creator.create("FitnessMax", base.Fitness, weights=(1.0,))
creator.create("Individual", list, fitness=creator.FitnessMax)

toolbox = base.Toolbox()
toolbox.register("attr_float", random.uniform, 0.0, 1.0)
toolbox.register("individual", tools.initRepeat, creator.Individual, toolbox.attr_float, n=128)
toolbox.register("population", tools.initRepeat, list, toolbox.individual)

# Hyperparameters
POPULATION_SIZE = 20
TOURNAMENT_SIZE = 3
CXPB = 0.7
MU = 0.0
SIGMA = 0.05
INDPB = 0.1
MAX_GENERATIONS = 100
TOTAL_RUNS = 10

toolbox.register("select", tools.selTournament, tournsize=TOURNAMENT_SIZE)
toolbox.register("mate", tools.cxTwoPoint)
toolbox.register("mutate", tools.mutGaussian, mu=MU, sigma=SIGMA, indpb=INDPB)

# State Management
current_run = 1
current_generation = 1
current_ind_index = 0
population = toolbox.population(n=POPULATION_SIZE)

# Data Collection
logbook = tools.Logbook()
logbook.header = ['gen', 'min', 'max', 'avg', 'std']
hof = tools.HallOfFame(10) # Save top 10 individuals across all generations

class FitnessReport(BaseModel):
    individual_id: int
    fitness_score: float
    generation: int

class GenomeResponse(BaseModel):
    individual_id: int
    dna: List[float]
    generation: int
    is_finished: bool

def save_run_data():
    """Exports Logbook to CSV and HoF to JSON for reusability."""
    os.makedirs("results", exist_ok=True)

    # Save CSV for plots
    csv_path = f"results/run_{current_run}_log.csv"
    with open(csv_path, "w", newline='') as f:
        writer = csv.DictWriter(f, fieldnames=logbook.header)
        writer.writeheader()
        writer.writerows(logbook)
    
    # Save JSON for Unity playback
    elites = [{"rank": i+1, "fitness": ind.fitness.values[0], "dna": list(ind)} for i, ind in enumerate(hof)]
    json_path = f"results/run_{current_run}_elites.json"
    with open(json_path, "w") as f:
        json.dump(elites, f, indent=4)
    
    print(f"Run {current_run} complete. Data saved to /results/.")

@app.get("/get-genome", response_model=GenomeResponse)
async def get_genome():
    global current_ind_index, current_generation, population, current_run, population, logbook, hof

    # Check if entire experiment is finished
    if current_run > TOTAL_RUNS:
        return GenomeResponse(individual_id=-1, dna=[], generation=current_generation, is_finished=True)
    
    # Check if current generation is finished
    if current_ind_index >= len(population):
        # Compile stats for finished generation
        fits = [ind.fitness.values[0] for ind in population]
        record = {
            'gen': current_generation,
            'min': float(np.min(fits)),
            'max': float(np.max(fits)),
            'avg': float(np.mean(fits)),
            'std': float(np.std(fits))
        }
        logbook.record(**record)
        hof.update(population)
        print(f"Gen {current_generation} Stats -> Max: {record['max']:.2f} | Avg: {record['avg']:.2f}")

        # Check if run is finished
        if current_generation >= MAX_GENERATIONS:
            save_run_data()
            current_run += 1

            if current_run > TOTAL_RUNS:
                return GenomeResponse(individual_id=-1, dna=[], generation=current_generation, is_finished=True)
            
            # Reset for next run
            current_generation = 1
            current_ind_index = 0
            population = toolbox.population(n=POPULATION_SIZE)
            logbook.clear()
            hof.clear()
            return await get_genome() # Fetch first genome of new run
        
        # Evolve next generation
        offspring = toolbox.select(population, len(population))
        offspring = [toolbox.clone(ind) for ind in offspring]

        for i in range(1, len(offspring), 2):
            if random.random() < CXPB:
                toolbox.mate(offspring[i - 1], offspring[i])
                del offspring[i - 1].fitness.values
                del offspring[i].fitness.values
        
        for mutant in offspring:
            toolbox.mutate(mutant)
            del mutant.fitness.values
        
        for ind in offspring:
            for i in range(len(ind)):
                ind[i] = max(0.0, min(1.0, ind[i]))
        
        population[:] = offspring
        current_generation += 1
        current_ind_index = 0

    ind = population[current_ind_index]
    return GenomeResponse(individual_id=current_ind_index, dna=list(ind), generation=current_generation, is_finished=False)

@app.post("/post-fitness")
async def post_fitness(report: FitnessReport):
    global current_ind_index

    # Reject scores from the wrong generation (catch race condition)
    if report.generation != current_generation:
        raise HTTPException(
            status_code=400,
            detail=f"Generation mismatch: expected {current_generation}, got {report.generation}"
        )
    
    # Reject scores for out-of-bounds IDs
    if report.individual_id < 0 or report.individual_id >= len(population):
        raise HTTPException(
            status_code=400,
            detail=f"individual_id {report.individual_id} out of range [0, {len(population) - 1}]"
        )

    # Assign the fitness to the DEAP individual
    population[report.individual_id].fitness.values = (report.fitness_score,)

    print(f"Gen {report.generation} | Ind {report.individual_id} scored: {report.fitness_score:.3f}")
    current_ind_index += 1

    return {"Status": "Success"}
