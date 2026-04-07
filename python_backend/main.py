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
toolbox.register("attr_float", random.uniform, 0.0, 1.0) # Easier to write to Unity as floats between 0 and 1, can be scaled in Unity
toolbox.register("individual", tools.initRepeat, creator.Individual, toolbox.attr_float, n=128)
toolbox.register("population", tools.initRepeat, list, toolbox.individual)

# TODO Dial in population size, mutation rate, crossover rate, and selection method
population = toolbox.population(n=20)
current_ind_index = 0

""" Generation Tracking """
current_generation = 1
max_generations = 100 # TODO This can be adjusted

""" API Models """
class FitnessReport(BaseModel):
    individual_id: int
    fitness_score: float
    generation: int # Unity sends this information too

class GenomeResponse(BaseModel):
    individual_id: int
    dna: List[float]
    generation: int
    is_finished: bool

""" Endpoints """
# Unity will call this to get the next genome to evaluate
@app.get("/get-genome", response_model=GenomeResponse)
async def get_genome():
    global current_ind_index, current_generation

    # Check if the current generation is finished
    if current_generation > max_generations:
        return GenomeResponse(
            individual_id=-1,
            dna=[],
            generation=current_generation,
            is_finished=True
        )

    if current_ind_index >= len(population):
        # TODO Insert DEAP evolutionary algorithm steps here (selection, crossover, mutation)
        # This block triggers when a generation is complete

        current_generation += 1
        current_ind_index = 0
    
    # TODO Fix for 422 Unprocessable Entity error - likely due to data format issues between Python and Unity
    ind = population[current_ind_index]
    return GenomeResponse(
        individual_id=current_ind_index,
        dna=list(ind),
        generation=current_generation,
        is_finished=False
    )

@app.post("/post-fitness")
async def post_fitness(report: FitnessReport):
    global current_ind_index

    # Assign the fitness to the DEAP individual
    population[report.individual_id].fitness.values = (report.fitness_score,)

    print(f"Gen {report.generation} | Ind {report.individual_id} scored: {report.fitness_score}")
    current_ind_index += 1

    return {"Status": "Success"}