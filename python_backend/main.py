from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List
import random
import numpy as np
from deap import base, creator, tools, algorithms

app = FastAPI(title="Body-Brain Co-evolution API")

""" DEAP setup """
# Fitness metric: Maximize distance traveled
# TODO Implement specific fitness function that considers multiple factors (details in Discord)
creator.create("FitnessMax", base.Fitness, weights=(1.0,))
creator.create("Individual", list, fitness=creator.FitnessMax)

toolbox = base.Toolbox()
# 64 floats for morphology 64 for neural weights and biases = 128 total
toolbox.register("attr_float", random.uniform, -1.0, 1.0)
toolbox.register("individual", tools.initRepeat, creator.Individual, toolbox.attr_float, n=128)
toolbox.register("population", tools.initRepeat, list, toolbox.individual)

# TODO Dial in population size, mutation rate, crossover rate, and selection method
population = toolbox.population(n=20)
current_ind_index = 0

""" API Models """
class FitnessReport(BaseModel):
    individual_id: int
    fitness_score: float

class GenomeResponse(BaseModel):
    individual_id: int
    dna: List[float]

""" Endpoints """
# Unity will call this to get the next genome to evaluate
# TODO Fix placeholder logic to trigger next generation after all individuals have been evaluated
@app.get("/get-genome", response_model=GenomeResponse)
async def get_genome():
    global current_ind_index
    if current_ind_index >= len(population):
        # TODO Trigger the next generation via crossover/mutation
        # For now, just loop for testing
        current_ind_index = 0
    
    ind = population[current_ind_index]
    return {"individual_id": current_ind_index, "dna": list(ind)}

# Unity will call this after the 10-second simulation is done
@app.post("/post-fitness")
async def post_fitness(report: FitnessReport):
    global current_ind_index

    # Assign the fitness to the DEAP individual
    population[report.individual_id].fitness.values = (report.fitness_score,)

    print(f"Received {report.individual_id} scored: {report.fitness_score}")
    current_ind_index += 1

    return {"status": "success", "next_steps": "Request next genome"}