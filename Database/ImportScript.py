import subprocess
import os
import json

# Function to run a command
def run_command(command):
    # print(f"Running command: {command}")
    subprocess.run(command, shell=True)
    
folder = os.getcwd()

print(folder)

for root, dirs, files in os.walk(folder):
    for file in files:
        if file.endswith('.json'):
            file_path = os.path.join(root, file)
            # print(f'Importing {file_path}')
            run_command(f'mongoimport --file=\"{file_path}\" --uri=\"mongodb://localhost:27017\" --db=\"SBOMDATA\" --collection=\"CVE\"')